# C# Search Service — Branch Documentation

Branch: `feature/csharp-search-service`

This document covers the full implementation, architecture, workflow, and
file-level changes introduced by this branch. See `CLAUDE.md` for hard
constraints (latency gates, no-UAC rule, resource ceiling).

---

## Background

The upstream Files app uses `Windows.Storage.Search` (AQS) for in-folder
search. That stack has a fixed per-query COM startup cost (~1–2 s) before
enumeration even begins, and falls back to a full directory walk when the
corpus is outside the Windows Search index. This branch introduces a sidecar
Windows Service with an in-memory inverted index to hit the CLAUDE.md gate
(≤10 % of legacy latency).

An earlier PoC built the service in Rust (Tantivy + tonic) on a separate
branch. This branch (`feature/csharp-search-service`) replaces that binary
with a pure C# service while keeping the same gRPC wire format and the
same `ISearchProvider` abstraction — removing the Rust toolchain dependency
and making the codebase fully maintainable by the existing C# team.

---

## High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Files.App  (WinUI 3, runs as the logged-in user)                │
│                                                                  │
│   SearchRouter                                                   │
│   ├── UseIndexed() == false  →  FolderSearch (legacy, upstream)  │
│   └── UseIndexed() == true   →  IndexedSearchProvider            │
│                                      │                           │
│                              named pipe: \\.\pipe\files-search   │
│                              gRPC / HTTP 2 (cleartext, local)    │
└──────────────────────────────────────────────────────────────────┘
                                      │
┌─────────────────────────────────────▼────────────────────────────┐
│  files-search-service.exe  (Windows Service, LocalSystem)        │
│                                                                  │
│  SearchGrpcService  ──►  FileIndex.Search()                      │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  FileIndex                                              │     │
│  │   _docs   — DocStore (parallel path/name/size/mtime     │     │
│  │              arrays, indexed by doc ID)                 │     │
│  │   _index  — Dictionary<token, int[]>                    │     │
│  │              posting lists, sorted, frozen per rebuild  │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                  │
│  IndexBootstrapper  ──►  UsnJournalReader  (initial build)       │
│  ChangeWatcher      ──►  EventBatcher      (live updates)        │
│  ProcessThrottle    ──►  battery/fullscreen/CPU guard            │
│  IndexPersistence   ──►  index.bin         (restart fast-load)   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Component Breakdown

### `src/Files.SearchService/` — the service

#### `Program.cs`

Entry point. Detects its execution context:

- **`!Environment.UserInteractive`** (started by SCM) → `ServiceBase.Run(new SearchWindowsService())`.
- **Interactive** (dev / console) → `RunAsync(CancellationToken)` directly (Ctrl+C to stop).

`RunAsync` does, in order:

1. `ProcessThrottle.ApplyBackgroundPriority()` — `PROCESS_MODE_BACKGROUND_BEGIN`.
2. `ProcessThrottle.StartPolling()` — 2-second timer for battery/fullscreen/CPU.
3. Resolve `FILES_SEARCH_ROOT` (env var → user profile → drive root when running as LocalSystem).
4. `IndexBootstrapper.BootstrapAsync()` — cold-start or reconcile (see below).
5. Start `ChangeWatcher` with an overflow handler that triggers a full rebuild.
6. Start a 5-minute `Timer` that persists the index to disk when dirty.
7. Build and start the Kestrel gRPC server on the named pipe `files-search`.

Named pipe DACL grants:

| Principal          | Rights      |
| ------------------ | ----------- |
| SYSTEM             | FullControl |
| Administrators     | FullControl |
| AuthenticatedUsers | ReadWrite   |

#### `SearchWindowsService.cs`

Thin `ServiceBase` shim. `OnStart` launches `Program.RunAsync` on a task;
`OnStop` cancels the token and waits up to 10 s for a clean shutdown.

Service metadata:

```
ServiceName:       FilesSearchService
CanStop:           true
CanPauseAndContinue: false
```

---

#### `Index/FileIndex.cs`

Thread-safe in-memory inverted index.

**Storage** — two volatile references swapped atomically on rebuild:

```
_docs   — DocStore  (four parallel arrays: paths, filenames, sizes, modified times)
_index  — Dictionary<string, int[]>  (token → sorted posting list)
```

**Writes** use `ReaderWriterLockSlim`. Reads snapshot both volatile
references without acquiring the lock — safe because references are
replaced atomically, never mutated in place after publication.

**ReplaceAll** (full rebuild):

```
records → Tokenizer.Tokenize(filename) for each
       → Dictionary<token, List<int>> accumulated
       → sorted int[] frozen into new _index
       → _docs replaced
```

**Upsert** (incremental):

```
Find existing doc for path → RemoveFromIndex (soft delete)
Add new doc id → InsertPosting (binary-search insert maintaining sorted order)
```

**Delete**: marks the doc ID as deleted in `DocStore`; posting lists are
cleaned lazily on next `ReplaceAll` to avoid O(n) per-delete work.

**Search** (lock-free):

```
Tokenize(query)
For each token:
    posting = _index[token]   ← missing token → return []
    hits = hits == null ? posting : Intersect(hits, posting)
Filter by scopePaths (prefix match, OrdinalIgnoreCase)
Score via Scorer.Score → sort descending → return up to maxResults
```

Intersection is a standard two-pointer merge on sorted `int[]` arrays —
O(min(|a|, |b|)) per token pair.

---

#### `Index/Tokenizer.cs`

Splits filenames into lowercase tokens:

1. Split on delimiter characters: ` . _ - ( ) [ ] + = & ,`
2. For each segment, split further on camelCase and digit/letter transitions:
   - Upper after lower → boundary (`MyDocument` → `my`, `document`)
   - Upper + next-lower after length > 1 → acronym end (`HTMLParser` → `html`, `parser`)
   - Letter → digit and digit → letter transitions

Example: `MyDocument_v2Final.docx` → `["my", "document", "v", "2", "final", "docx"]`

**Known gap:** mid-string substrings are not matched. Searching `phab` will
not find `ALPHABET.md` because `phab` is not a token. This is tracked as a
roadmap item (n-gram field).

---

#### `Index/IndexBootstrapper.cs`

Handles two startup paths:

**Cold start** (no `index.bin`):

```
UsnJournalReader.Enumerate() → List<DocRecord>
FileIndex.ReplaceAll(records)
IndexPersistence.SaveAsync(persistPath, records)
```

**Warm start** (existing `index.bin`):

```
IndexPersistence.LoadAsync()  → persisted records
FileIndex.ReplaceAll(persisted)     ← service can answer queries immediately
UsnJournalReader.Enumerate()  → fsMap  (runs in parallel)
Diff persisted vs fsMap:
    new or modified → index.Upsert()
    deleted         → index.Delete()
Re-persist reconciled state
```

The warm-start design lets the service answer queries from the cached index
within milliseconds of startup, even before the reconcile walk finishes.

---

#### `Index/IndexPersistence.cs`

Binary format (`index.bin`):

```
[4 bytes]  magic: 0x46534958 ("FSIX")
[4 bytes]  version: 1
[8 bytes]  record count
Per record:
  [8 bytes]  size_bytes (u64)
  [8 bytes]  modified_unix_ms (FILETIME)
  [4 bytes]  full_path UTF-8 length
  [N bytes]  full_path UTF-8
  [4 bytes]  file_name UTF-8 length
  [N bytes]  file_name UTF-8
```

Writes are atomic: temp file written then renamed over the target.
Version mismatch on load triggers a full rebuild (not a crash).

---

#### `Usn/UsnJournalReader.cs`

Enumerates every file on an NTFS volume using `FSCTL_ENUM_USN_DATA`.

**USN path** (requires LocalSystem / `SeBackupPrivilege`):

```
OpenVolumeHandle(\\.\C:)
ParseMft():
    DeviceIoControl(FSCTL_ENUM_USN_DATA) in 256 KB chunks
    → dirs  : Dictionary<ulong FRN, DirEntry>
    → files : List<FileRecord>
Parallel.ForEach(files):
    ResolvePath() — walk parent-FRN chain up to rootFrn
    → FileEntry(fullPath, fileName, size=0, timestamp)
```

Path resolution walks the `dirs` dictionary up the FRN parent chain,
limited to 64 hops as a cycle guard. Files not under `root` are dropped.

Note: USN records carry size as 0 (it's a metadata-only log); the watcher
fills accurate sizes in on the next file-change event.

**Fallback path** (dev / non-LocalSystem): `DirectoryInfo.EnumerateFiles`
with `RecurseSubdirectories=true`, `AttributesToSkip=ReparsePoint`.

---

#### `Watch/ChangeWatcher.cs` + `Watch/EventBatcher.cs`

`ChangeWatcher` wraps `FileSystemWatcher` (which uses `ReadDirectoryChangesW`
on Windows). Events are forwarded to `EventBatcher`.

`EventBatcher` coalesces bursts via a 250 ms debounce:

```
Enqueue(change):
    _pending[path] = change   ← last event wins (delete after create = delete)
    reset 250 ms timer

Flush() (on timer):
    batch = _pending.Values
    _pending.Clear()
    ApplyBatch(batch)
```

`ApplyBatch` stats each upsert path (`FileInfo`) and calls
`FileIndex.Upsert` or `FileIndex.Delete`. Reparse points and directories
are skipped. IOExceptions (race between event and file deletion) are
swallowed.

**Overflow**: if `ReadDirectoryChangesW`'s internal kernel buffer overflows
(burst too large), `ChangeWatcher.Overflow` fires. `Program.RunAsync`
handles this by stopping the watcher, running a full `BootstrapAsync`, then
restarting — no events are permanently lost.

---

#### `Throttle/ProcessThrottle.cs`

Sets `PROCESS_MODE_BACKGROUND_BEGIN` once at startup, lowering the
process's CPU and I/O scheduling priority below normal.

Polls every 2 seconds for three conditions:

| Condition  | Win32 API                        | Threshold             |
| ---------- | -------------------------------- | --------------------- |
| On battery | `GetSystemPowerStatus`         | `ACLineStatus == 0` |
| Fullscreen | `SHQueryUserNotificationState` | states 3 or 4         |
| CPU high   | `GetSystemTimes` delta         | > 70 %                |

`ShouldPause()` returns a `volatile bool`. The watcher's commit loop
(EventBatcher flush → FileIndex.Upsert) skips the index-publish step
while paused — events are still enqueued, just not committed to the index
until conditions improve.

---

#### `Grpc/SearchGrpcService.cs`

Implements the generated `FilesSearch.FilesSearchBase`:

- **`Health`** — returns version, `DocCount`, and `IsIndexing` flag.
- **`Search`** — calls `FileIndex.Search(query, maxResults, scopePaths)`,
  streams each `QueryHit` back as a `SearchHit` proto message.
  Checks cancellation between messages.

---

#### `proto/files_search.proto`

Single source of truth for the wire format, shared between the C# service
and `Files.IndexedSearch.Client` (Grpc.Tools generates stubs from this file).

```protobuf
service FilesSearch {
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc Search(SearchRequest) returns (stream SearchHit);
}
```

`SearchRequest` carries `query`, `max_results`, and a repeated
`scope_paths` field (full directory paths the results must be prefixed by).

---

### `src/Files.IndexedSearch.Client/` — the C# client

`IndexedSearchProvider` implements `ISearchProvider` over the named pipe.

**Channel construction** (lazy, reused for provider lifetime):

```
FILES_SEARCH_SERVICE_URL set? → GrpcChannel.ForAddress(url)  [TCP, dev/CI]
Otherwise:
    SocketsHttpHandler { ConnectCallback = NamedPipeClientStream("files-search") }
    GrpcChannel.ForAddress("http://localhost", handler)       [named pipe]
```

The dummy `http://localhost` URI satisfies gRPC's URI requirement; the
transport is actually the named pipe.

**`SearchAsync`**: builds a `SearchRequest`, opens a server-streaming call,
yields each `SearchHit` as a `SearchResult` via `IAsyncEnumerable`.

**`GetHealthAsync`**: catches `RpcException` and returns
`IsAvailable=false` — the routing layer never needs try/catch.

---

### `src/Files.App/` — app-side changes

#### `Utils/Storage/Search/SearchRouter.cs`

Drop-in replacement for `FolderSearch`. Routing logic:

```
UseIndexed():
    1. settings.GeneralSettingsService.UseIndexedSearch OR
       env FILES_SEARCH_PROVIDER=Indexed         → enabled
    2. query is null or empty                    → legacy
    3. query contains * or ?                     → legacy  (glob)
    4. query starts with $                       → legacy  (AQS prefix)
    5. query contains :                          → legacy  (AQS field)
    6. folder is null, "Home", or a library      → legacy
    → indexed

SearchIndexedAsync():
    GetHealthAsync()        → if unavailable, fall back to legacy
    FileIndex.Search()      → stream results
    Fire SearchTick at 32 results, then every 300
    ToListedItem():
        No StorageFile.GetFileFromPathAsync round-trip
        Creation time = ModifiedUtc (v0 fidelity trade-off)
```

#### `Helpers/Application/SearchServiceManager.cs`

Called fire-and-forget from `AppLifecycleHelper` at startup.

```
IsPackaged()?
  true  → ServiceController("FilesSearchService").Start() if stopped
  false → RegisterStartup(HKCU\Run) + LaunchIfNotRunning(files-search-service.exe)
```

Dev mode locates the exe via `AppContext.BaseDirectory`; packaged mode via
`Package.Current.InstalledLocation`.

#### `Package.appxmanifest`

```xml
<desktop6:Extension Category="windows.service"
    Executable="files-search-service.exe"
    EntryPoint="windows.FullTrustApplication">
  <desktop6:Service Name="FilesSearchService"
      StartType="auto"
      StartAccount="localSystem" />
</desktop6:Extension>
```

SCM installs and auto-starts the service at package install time (already
elevated). No UAC prompt at runtime, ever.

#### Settings UI (`AdvancedPage.xaml`, `AdvancedViewModel.cs`, `GeneralSettingsService.cs`)

New `UseIndexedSearch` boolean setting, surfaced as a `ToggleSwitch` in
**Settings → Advanced** with strings `SettingsUseIndexedSearch` /
`SettingsUseIndexedSearchDescription`. The setting persists via the
existing `IGeneralSettingsService` store and is read by `SearchRouter.UseIndexed()`.

---

## Startup Sequence

```
Windows login
     │
     ├─ SCM reads MSIX manifest
     │       └─ auto-starts FilesSearchService as LocalSystem
     │
     └─ Files.App starts (user session)
             │
             ├─ AppLifecycleHelper.InitializeAsync()
             │       └─ Task.Run(SearchServiceManager.EnsureRunning)
             │               └─ (packaged) ServiceController.Start() if stopped
             │
             └─ User types in search box
                     │
                     └─ SearchRouter.SearchAsync()
                             ├─ UseIndexed() == false → FolderSearch (legacy)
                             └─ UseIndexed() == true
                                     └─ IndexedSearchProvider.GetHealthAsync()
                                             ├─ unavailable → FolderSearch fallback
                                             └─ available
                                                     └─ stream results from FileIndex
```

---

## Service Startup Sequence

```
Program.RunAsync()
     │
     ├─ ProcessThrottle.ApplyBackgroundPriority()
     ├─ ProcessThrottle.StartPolling()
     │
     ├─ ResolveRoot()  (FILES_SEARCH_ROOT → %USERPROFILE% → C:\)
     │
     ├─ IndexBootstrapper.BootstrapAsync()
     │       ├─ index.bin exists?
     │       │       yes → LoadAsync() → ReplaceAll() [queries live immediately]
     │       │               └─ UsnJournalReader.Enumerate() → diff → upsert/delete
     │       └─ no  → UsnJournalReader.Enumerate() → ReplaceAll() → SaveAsync()
     │
     ├─ ChangeWatcher.Start()
     │       └─ FileSystemWatcher (ReadDirectoryChangesW)
     │               └─ EventBatcher (250 ms debounce)
     │                       └─ FileIndex.Upsert / Delete
     │
     ├─ periodic save Timer (every 5 min, when dirty)
     │
     └─ Kestrel gRPC server
             └─ named pipe: \\.\pipe\files-search
                     └─ SearchGrpcService
```

---

## Query Routing Decision Tree

```
User types query "report"
         │
         ▼
SearchRouter.UseIndexed()
         │
    enabled? ──No──► FolderSearch (legacy AQS)
         │
        Yes
         │
    query empty? ──Yes──► legacy
         │
    glob chars (* ?)? ──Yes──► legacy
         │
    AQS prefix ($)? ──Yes──► legacy
         │
    AQS field (:)? ──Yes──► legacy
         │
    real on-disk folder? ──No──► legacy
         │
        Yes
         │
    GetHealthAsync() ──unavailable──► legacy fallback
         │
      available
         │
         ▼
    FileIndex.Search("report", maxResults, [folder])
         │
    Tokenize("report") → ["report"]
         │
    posting = _index["report"]   (e.g. 1 847 doc IDs)
         │
    filter by scope prefix
         │
    score → sort → stream to UI
```

---

## Data Flow: Inverted Index Build

```
UsnJournalReader
     │
     │  FSCTL_ENUM_USN_DATA (256 KB chunks)
     │  → USN_RECORD_V2 for every MFT entry
     │  → dirs dict (FRN → parent FRN + name)
     │  → files list (FRN, parent FRN, name, timestamp)
     │
     │  Parallel.ForEach(files):
     │    ResolvePath(parentFrn, fileName, rootFrn)
     │    → walk parent-FRN chain → full path
     │
     ▼
List<DocRecord>(fullPath, fileName, sizeBytes=0, modifiedUtc)
     │
     ▼
FileIndex.ReplaceAll()
     │
     │  for each record:
     │    DocStore.Add(path, name, size, mtime) → docId
     │    Tokenizer.Tokenize(name) → tokens
     │    for each token: index[token].Add(docId)
     │
     │  Convert List<int> → sorted int[] (posting lists)
     │
     ▼
_index  : Dictionary<string, int[]>   ~volatile snapshot
_docs   : DocStore                    ~volatile snapshot
```

---

## Project Layout Changes

```
Files.slnx
 └─ added: src/Files.SearchService/
            src/Files.SearchAbstraction/   (ISearchProvider interface)
            src/Files.LegacySearch/        (AQS wrapper, frozen)
            src/Files.IndexedSearch.Client/
            tests/Files.Search.Bench/
            tests/Files.Search.Correctness/

New files (untracked or new):
  src/Files.SearchService/           ← the service (new project)
  src/Files.App/Helpers/Application/SearchServiceManager.cs
  src/Files.App/files-search-service.exe   (build output, dev mode)
  tests/Files.Search.Correctness/    ← correctness harness scaffold
  run-bench.ps1                      ← one-shot build + bench + gate check
  .smoke/                            ← smoke test artifacts
```

---

## Files Changed (branch diff vs. `main`)

| File                                                          | Change                                                        |
| ------------------------------------------------------------- | ------------------------------------------------------------- |
| `CLAUDE.md`                                                 | Added C# service architecture, updated workflow               |
| `Directory.Packages.props`                                  | Pinned Grpc, Grpc.AspNetCore, Grpc.Tools versions             |
| `Files.slnx`                                                | Added four new projects                                       |
| `docs/search-roadmap.md`                                    | Current C# service status snapshot                            |
| `src/Files.App/Data/Contracts/IGeneralSettingsService.cs`   | Added `UseIndexedSearch` property                           |
| `src/Files.App/Services/Settings/GeneralSettingsService.cs` | Implemented `UseIndexedSearch`                              |
| `src/Files.App/Strings/en-US/Resources.resw`                | Added two string resources for settings UI                    |
| `src/Files.App/Views/Settings/AdvancedPage.xaml`            | Added indexed search toggle card                              |
| `src/Files.App/ViewModels/Settings/AdvancedViewModel.cs`    | Added `UseIndexedSearch` VM property                        |
| `src/Files.App/Utils/Storage/Search/SearchRouter.cs`        | New: routing logic, health probe, indexed path                |
| `src/Files.App/Helpers/Application/AppLifecycleHelper.cs`   | Fire-and-forget `SearchServiceManager.EnsureRunning`        |
| `src/Files.App/Package.appxmanifest`                        | `desktop6:Service` declaration                              |
| `src/Files.App/Files.App.csproj`                            | Project references +`files-search-service.exe` content item |
| `src/Files.IndexedSearch.Client/IndexedSearchProvider.cs`   | Named-pipe channel,`IAsyncEnumerable` streaming             |

New projects (untracked in git diff, shown as `??`):

| Path                                | Purpose                                        |
| ----------------------------------- | ---------------------------------------------- |
| `src/Files.SearchService/`        | The Windows Service (C#)                       |
| `tests/Files.Search.Correctness/` | Correctness harness scaffold                   |
| `run-bench.ps1`                   | Build + start service + run bench + gate check |

---

## Benchmark Results (small corpus, 50 k files)

All runs against `.bench/small/` (50 k files, ~2.8 GB, seed=42).

| Date       | Provider               | TTFR p50 | TTFR p99 | Total p50 | Total p99 |
| ---------- | ---------------------- | -------- | -------- | --------- | --------- |
| 2026-05-10 | legacy AQS (5 k files) | 2025 ms  | —       | 2380 ms   | —        |
| 2026-05-10 | indexed (5 k files)    | 3 ms     | —       | 4 ms      | —        |
| 2026-05-11 | indexed (50 k)         | 11 ms    | 174 ms   | 38 ms     | 189 ms    |
| 2026-05-12 | naive-scan (50 k)      | ~0 ms*   | 48 ms    | 44 ms     | 8329 ms   |
| 2026-05-12 | indexed (50 k)         | 11 ms    | 88 ms    | 40 ms     | 210 ms    |

\* naive-scan TTFR≈0 ms is misleading: substring queries match the first file
in directory order immediately; indexed has an 11 ms gRPC named-pipe floor.

**Gate results** (CLAUDE.md, vs. legacy AQS baseline):

| Gate                     | Target | Result                      |
| ------------------------ | ------ | --------------------------- |
| TTFR median vs. legacy   | ≤10 % | 0.5 % (11 ms / 2025 ms) ✓  |
| Total p99 vs. naive-scan | —     | 2.5 % (210 ms / 8329 ms) ✓ |

Pinned baseline: `bench-results/baseline.json` (2026-05-12).

---

## Known Gaps / Roadmap

| Gap                                                    | Status                                                                    |
| ------------------------------------------------------ | ------------------------------------------------------------------------- |
| Mid-string substring (e.g.`phab` → `ALPHABET.md`) | Not implemented; needs n-gram field                                       |
| Glob queries (`*.txt`, `report*`)                  | Fall back to legacy via `SearchRouter`                                  |
| Content search                                         | Not implemented (v0 ships filename index only)                            |
| Library and Home scopes                                | Fall back to legacy (need fan-out logic)                                  |
| Named-pipe ACL smoke test                              | Deferred until packaged build can be tested end-to-end                    |
| Index location under packaged identity                 | To be confirmed via packaged smoke test                                   |
| Offline change reconcile                               | Covered by `IndexBootstrapper.LoadAndReconcileAsync` on service restart |

---

## Running Locally (Dev Mode)

```powershell
# 1. Generate the small corpus (one-time)
dotnet run --project tests\corpora -- --preset small --out .bench\small

# 2. Full bench: build, start service, run naive-scan + indexed, gate check
.\run-bench.ps1

# Optional flags:
#   -SkipBuild      skip dotnet build (service and bench already built)
#   -NoNaiveScan    skip the slow naive-scan baseline run
#   -Corpus <path>  use a different corpus directory

# Run the service manually (dev console mode):
$env:FILES_SEARCH_ROOT      = ".bench\small"
$env:FILES_SEARCH_INDEX_DIR = ".bench\index"
dotnet run --project src\Files.SearchService -c Release
```

The service detects that it is not started by SCM (`Environment.UserInteractive == true`)
and runs in console mode. Press Ctrl+C for a clean shutdown with a final
index persist.

To route Files.App to the indexed provider without the settings UI, set the
environment variable before launching Files:

```powershell
$env:FILES_SEARCH_PROVIDER = "Indexed"
# then launch Files.App from Visual Studio or msix
```
