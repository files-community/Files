# CLAUDE.md

Fork of [files-community/Files](https://github.com/files-community/Files) (C#/WinUI 3). Goal: faster, AI-augmented search without regressing the rest.

## Search goals (hard constraints)

1. **Faster.** Query latency ≤10% of Windows Search on equivalent corpora.
2. **No heavier.** RAM/disk/idle CPU ≤ upstream Files + Windows Search Indexer.
3. **No runtime UAC.** No admin prompts during normal use. The service is declared as a `desktop6:Service` in the MSIX manifest and installed by Windows at package install time (which already runs elevated). SCM manages it from there — no UAC at launch, ever.

## Architecture

`files-search-service.exe` is a pure C# Windows Service (`src/Files.SearchService/`) installed via the MSIX manifest (`desktop6:Service`, `StartAccount=localSystem`). SCM starts it at login. Files.App is a pure gRPC client over a named pipe — it never spawns or owns the process in packaged mode.

- **Enumeration (initial):** NTFS USN Change Journal via `FSCTL_ENUM_USN_DATA` — reads the kernel's file-change log directly, indexes millions of files in seconds. Requires LocalSystem, provided by the SCM service registration.
- **Enumeration (fallback):** `DirectoryInfo.EnumerateFiles` with `RecurseSubdirectories=true`, `AttributesToSkip=ReparsePoint`. Used in dev/unpackaged mode when the volume handle can't be opened.
- **Updates:** `FileSystemWatcher` (`ReadDirectoryChangesW` under the hood), recursive, 250ms debounced commits via `EventBatcher`. Overflow triggers a full rebuild.
- **Throttle:** `PROCESS_MODE_BACKGROUND_BEGIN` at startup; 2s polling pauses commits on battery / fullscreen / high CPU.
- **Index:** in-memory inverted index (`Dictionary<string, int[]>` posting lists, atomically swapped on rebuild) + trigram index for mid-string substrings. Filename-only in v0. Persisted to `index.bin` (custom binary format, magic `FSIX`) for fast restart with reconcile-against-disk diff.
- **Transport:** gRPC over named pipe `\\.\pipe\files-search` (Kestrel `ListenNamedPipe`). TCP loopback available via `FILES_SEARCH_SERVICE_URL` for dev/CI.

## Coexistence

All search goes through `ISearchProvider`. Two impls ship:

- `LegacySearchProvider` — wraps upstream unchanged. Frozen reference; instrumentation only.
- `IndexedSearchProvider` — talks to the new service.

Selected by `UseIndexedSearch` setting (Settings → Advanced) → env var `FILES_SEARCH_PROVIDER` → default. Default stays `Legacy` until benchmarks pass. `SearchRouter` falls back to legacy for glob (`*`/`?`), AQS (`$`/`:`), Home/library scopes, or when the service is unavailable.

## Layout

```
src/Files.App/                       UI, modified only to consume ISearchProvider
src/Files.SearchAbstraction/         interface + types
src/Files.LegacySearch/              upstream wrapper
src/Files.IndexedSearch.Client/      C# gRPC client
src/Files.SearchService/             C# Windows Service (the indexer)
tests/Files.Search.Correctness/      result equivalence
tests/Files.Search.Bench/            perf benchmarks
tests/Files.Search.Probe/            console probe / smoke harness
tests/corpora/                       deterministic corpus generators
```

## Tests

**Correctness.** For each `(corpus, query)`, indexed results ⊇ legacy results (modulo documented exclusions). Cases: exact, glob, substring, ext+substring, content, path-scoped, unicode, long paths, hidden/system/symlinks.

**Benchmarks.** Three corpora generated deterministically: `small` (50k files, ~2GB), `medium` (500k, ~50GB), `large` (2M, ~500GB). ~200 queries per corpus. Per `(provider, corpus, query)` record: time-to-first-result, time-to-complete, peak RAM, CPU-seconds, bytes read. Indexing also tracks: cold-start time, steady-state RAM, index size on disk, incremental update latency. JSON to `bench-results/<timestamp>.json`. `run-bench.ps1` at repo root is the one-shot driver.

**Acceptance gates** (vs. legacy baseline on `medium`):

| Metric | Target |
|---|---|
| Time-to-first-result, median | ≤10% of legacy |
| Time-to-first-result, p99 | ≤15% of legacy |
| Steady-state RAM | ≤100% of legacy + indexer |
| Idle CPU (60s post-index) | ≤ legacy + indexer |
| Initial index time | ≤2x Windows Search |
| Incremental update p95 | ≤5s |

Baseline pinned in `bench-results/baseline.json`, updated only by explicit decision.

**Resource (nightly).** Battery/fullscreen/load throttling verified. No handle leaks over 1h. No memory growth over 24h soak.

## Workflow

- Correctness suite runs per-commit. Regressions block merge.
- `Bench --corpus small` per-commit; `medium` nightly.
- Legacy provider is frozen — instrumentation and upstream-mirrored bugfixes only.

## See also

- `docs/csharp-search-service.md` — full component-level architecture and file map.
- `docs/decisions/0001-bench-stack.md` — bench harness choice.
- `docs/decisions/0003-bench-strategy-theoretical.md` — Big-O-for-gates rationale.
- `docs/search-roadmap.md` — current status snapshot.
