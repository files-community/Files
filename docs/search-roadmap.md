# Search rewrite — roadmap

Status snapshot, kept short. Update inline as state changes; don't append a
log. CLAUDE.md has the architecture; this file is just *where we are*.

## Done

- ADR 0001 — bench stack chosen.
- ADR 0002 — Rust service transport: TCP for v0, named pipe later.
- ADR 0003 — bench strategy: Big O for the gates, empirical for
  constants and regressions. `small` is the canonical baseline; `medium`
  and `large` are gated on Windows Search Indexer integration first.
- `tests/corpora/` and `tests/Files.Search.Bench/` scaffolds exist.
- `src/search-service/` skeleton: tonic gRPC server on `127.0.0.1:50080`,
  vendored protoc, pinned to Rust 1.95. `FilesSearch` service with
  `Health` + streaming `Search` (returns empty stream).
- End-to-end signal: `lib.rs` split, `tests/search_smoke.rs` exercises
  Health + Search via a real tonic client over an ephemeral TCP port.
- Tantivy filename index in `src/index.rs`, on-disk persistence at
  `%LOCALAPPDATA%\Files\search-index\` (override via
  `FILES_SEARCH_INDEX_DIR`). Schema: `path` STRING, `filename` TEXT,
  `size_bytes` U64, `modified_unix_ms` I64. Per-token prefix queries via
  `FuzzyTermQuery::new_prefix(_, 0, _)`; `scope_paths` becomes a
  path-prefix filter clause.
- Enumerator in `src/enumerate.rs` — Windows path uses `FindFirstFileExW`
  with `FindExInfoBasic` + `FIND_FIRST_EX_LARGE_FETCH`, recursion fanned
  out via `rayon::scope`, entries streamed through an `mpsc::Sender` so
  the Tantivy writer drains concurrently. Reparse points skipped to
  match `WalkDir(follow_links=false)` semantics. `walkdir` fallback
  `#[cfg(not(windows))]` keeps the crate portable for dev.
- Watcher in `src/watcher.rs` — wraps the `notify` crate (which uses
  `ReadDirectoryChangesW` + overlapped I/O on Windows). `SearchIndex`
  now holds its writer behind a `parking_lot::Mutex` so the watcher
  can `upsert`/`delete` without recreating it. Commits are debounced
  on a 250ms quiet window so bursts (`git checkout`, archive extract)
  collapse into a single Tantivy commit. Final commit on shutdown.
- Throttle in `src/throttle.rs` — `apply_background_priority()` calls
  `SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN)` once at startup
  (lowers CPU + I/O priority below normal). `Throttle` polls every 2s
  for battery (`GetSystemPowerStatus`), fullscreen
  (`SHQueryUserNotificationState`), and CPU load (`GetSystemTimes`,
  threshold 70%); the watcher's commit loop skips commits while
  `should_pause()` is true so query-visibility (and fsync) defers
  until idle. Apply work still happens so events aren't lost.
  12/12 Rust tests green.
- C# `Files.SearchAbstraction` defined: `ISearchProvider` (streaming
  `IAsyncEnumerable<SearchResult>` + `GetHealthAsync`), `SearchQuery`,
  `SearchResult`, `HealthStatus`. `net10.0` (no Windows deps) so any
  consumer can reference it. Registered in `Files.slnx` under
  `/src/core/`.
- `Files.LegacySearch` — `LegacySearchProvider` implements
  `ISearchProvider` over `Windows.Storage.Search` (AQS via
  `QueryOptions { FolderDepth.Deep, IndexerOption.UseIndexerWhenAvailable,
  SortBy System.Search.Rank desc }`). AQS construction mirrors
  upstream's `FolderSearch.AQSQuery` ($/colon/dot-aware wildcard
  cases). Builds in batches of 500 via `CreateFileQueryWithOptions`,
  yields per file. Cancellation honored at every batch boundary;
  per-file stat failures swallowed to match upstream.
- `Files.IndexedSearch.Client` — `IndexedSearchProvider` implements
  `ISearchProvider` over gRPC. `Grpc.Tools` generates client stubs
  from the *same* `src/search-service/proto/files_search.proto` the
  Rust service consumes, so the wire format has a single source of
  truth. Single persistent `GrpcChannel` (HTTP/2 multiplexes calls).
  TCP `127.0.0.1:50080` default; override via
  `FILES_SEARCH_SERVICE_URL`. `GetHealthAsync` translates transport
  failure into `IsAvailable=false` so the routing layer doesn't need
  try/catch around every probe.
- Bench harness in `tests/Files.Search.Bench/` wired up: existing
  scaffold (200-query generator, JSON output, machine info) now sees
  three providers — `naive-scan`, `legacy`, `indexed`. Adapter maps
  bench `Query` → `SearchQuery(text, [corpusRoot])` so each provider
  searches the same tree regardless of its default scope. One warm-up
  query per run absorbs JIT / gRPC channel / Tantivy mmap penalties.
  Aggregate `Aggregates { ttfrMedianMs, ttfrP95Ms, ttfrP99Ms,
  totalMedianMs, totalP95Ms, totalP99Ms }` block added to JSON output
  so gates in CLAUDE.md can be diffed against `bench-results/baseline.json`
  directly.

## First bench run, 5k smoke corpus (2026-05-10)

Calibration run only — `small` will be the canonical baseline (see ADR 0003).

| Class            | Legacy hits | Indexed hits | Legacy p50 | Indexed p50 | Speedup |
|------------------|------------:|-------------:|-----------:|------------:|--------:|
| substring        |       175.5 |        177.2 |    2380 ms |        4 ms |    595× |
| glob             |       311.8 |          0.0 |    3363 ms |        3 ms |   1121× |
| exact            |         0.0 |          0.0 |    1120 ms |        3 ms |    373× |
| ext+substring    |         0.0 |          0.0 |    1095 ms |        3 ms |    365× |
| content          |         0.0 |          0.0 |    1084 ms |        3 ms |    361× |

Indexed beats the ≤10% gate by 3 orders of magnitude on every class it
answers. `glob` is the headline correctness gap (Tantivy doesn't do
globs); needs routing-layer policy to fall back to legacy on `*` / `?`.

Bug shaken out: indexed paths used forward slashes (from
`FILES_SEARCH_ROOT="C:/..."`) while C# scope used backslashes (from
`Path.GetFullPath`); prefix match silently returned 0 hits. Fixed in
`main.rs::normalize_root`.

- `Files.App` wired to the new search stack. Added project references
  to `Files.SearchAbstraction`, `Files.LegacySearch`,
  `Files.IndexedSearch.Client`. New `SearchRouter` in
  `src/Files.App/Utils/Storage/Search/SearchRouter.cs` is a drop-in
  replacement for `FolderSearch` (same `Query`/`Folder`/`MaxItemCount`
  properties, same `SearchTick` event, same `SearchAsync(IList<ListedItem>,
  CancellationToken)` shape). Routing is opt-in via
  `FILES_SEARCH_PROVIDER=Indexed` env var; default behavior is
  byte-identical to legacy. Indexed path also requires a non-glob,
  non-AQS query and a real on-disk folder (not "Home", not a library)
  — anything else falls back to legacy. Service-down gracefully falls
  back via `IndexedSearchProvider.GetHealthAsync()`. Migrated four
  call sites: `ShellViewModel.SearchAsync`,
  `NavigationToolbarViewModel`, `BaseShellPage`, `BaseLayoutPage`.
  C# compiles clean.

## Next, in order

1. **Service launcher** — small helper that starts
   `files-search-service.exe` as a child process when the indexed path
   is selected, and stops it on app exit. Currently the user must
   start the service manually.
2. **Swap TCP → named pipe (`\\.\pipe\files-search`).** Custom tonic
   Connector/Acceptor over `tokio::net::windows::named_pipe`, plus the
   matching named-pipe channel in the C# client.
3. **Content + semantic indexes** — Tantivy content fields, then HNSW.
   Off the critical path until filename search is shipping.

Running `medium` / `large` empirically is deferred per ADR 0003 until
the corpus can be added to Windows Search Indexer's catalog.

## Known gaps

- Tantivy's default tokenizer + per-token prefix matches whole-word and
  prefix queries (`alpha` finds `alpha.txt` and `ALPHABET.md`) but not
  mid-string substrings (`phab` does not find `ALPHABET.md`). Revisit
  with an n-gram field if the correctness suite demands legacy parity.
- The watcher closes the live-update gap, but changes that happen while
  the service is *offline* still leave the index stale until something
  triggers a rebuild. Restart-time reconcile (walk root, diff against
  index, apply deltas) is not implemented yet.

## Parallel C# work (no Rust dependency)

- Define `Files.SearchAbstraction` (`ISearchProvider` + types). Unblocks
  both `Files.LegacySearch` and `Files.IndexedSearch.Client`.
- `Files.LegacySearch` — wrap upstream search behind `ISearchProvider`.
  Frozen reference per CLAUDE.md.
- Flesh out corpus generators (`tests/corpora/`) and bench harness
  (`tests/Files.Search.Bench/`) toward the JSON output schema and the
  acceptance-gate metrics in CLAUDE.md.

## Open questions

- Named-pipe ACL: default (creator only) is right, but confirm the C#
  client running in the packaged app can open it.
- Index location under packaged identity vs. unpackaged dev runs.
- Whether the service is launched on demand by `Files.App` or runs as a
  user-scoped scheduled task. Affects cold-start measurement.
