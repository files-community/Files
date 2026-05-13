# Search rewrite — roadmap

Status snapshot for the C# search service on `feature/csharp-search-service`.
`CLAUDE.md` has the constraints; `docs/csharp-search-service.md` has the
full architecture and file map. This file is just *where we are*.

## Done

- `Files.SearchAbstraction` — `ISearchProvider`, `SearchQuery`, `SearchResult`,
  `HealthStatus` (`net10.0`, no Windows deps).
- `Files.LegacySearch` — `LegacySearchProvider` wraps `Windows.Storage.Search`
  (AQS) behind `ISearchProvider`. Frozen reference.
- `Files.IndexedSearch.Client` — gRPC client over named pipe
  (`\\.\pipe\files-search`); TCP loopback fallback via
  `FILES_SEARCH_SERVICE_URL`. Stubs generated from
  `src/Files.SearchService/proto/files_search.proto` (single source of truth).
- `Files.SearchService` — C# Windows Service. In-memory inverted index
  (`Dictionary<string, int[]>` posting lists, atomic swap on rebuild) +
  trigram index for mid-string substrings. `DocStore` parallel arrays.
  `IndexBootstrapper` does USN-or-fallback enumeration with warm-start
  reconcile against `index.bin`. `ChangeWatcher` + `EventBatcher` 250ms
  debounce. `ProcessThrottle` background priority + battery/fullscreen/CPU
  polling. Kestrel gRPC on named pipe with DACL granting AuthenticatedUsers RW.
- `Files.App` — `SearchRouter` drop-in for `FolderSearch`. Settings UI toggle
  `UseIndexedSearch` in Settings → Advanced. `SearchServiceManager` ensures
  the service is running (SCM in packaged mode; HKCU\Run + direct launch
  in dev).
- `Package.appxmanifest` — `desktop6:Service`, `StartAccount=localSystem`,
  `StartType=auto`.
- Bench harness: `run-bench.ps1` (build → start service → run bench →
  gate check). `naive-scan`, `legacy`, `indexed` providers in
  `tests/Files.Search.Bench/`.

## Bench, small corpus (50k files, 2026-05-12)

`bench-results/baseline.json` — pinned.

| Provider     | TTFR p50 | TTFR p99 | Total p50 | Total p99 |
|--------------|---------:|---------:|----------:|----------:|
| legacy AQS\* |  2025 ms |        — |   2380 ms |         — |
| indexed      |    11 ms |    88 ms |     40 ms |    210 ms |
| naive-scan   |   ~0 ms  |    48 ms |     44 ms |   8329 ms |

\* Legacy AQS measured on the 5k smoke run; full 50k legacy run deferred per
ADR 0003 (≥80 min wall time on a corpus outside the Windows Search Indexer
catalog tells us nothing new).

**Gate result:** TTFR median 11 ms / 2025 ms = 0.5% (gate: ≤10%). ✓

## Next, in order

See `memory/project_search_pr_punchlist.md` for the full P0/P1/P2 list
before sending to the Files team. Highlights:

**P0 — blocking PR**
1. Validate packaged SCM path end-to-end (named pipe + LocalSystem).
   Dev mode (TCP) works; packaged path never verified on this machine.
2. Commit `tests/Files.Search.Correctness/`, `tests/Files.Search.Bench/`,
   and `tests/Files.Search.Probe/` (currently untracked).
**P1 — quality**
4. Index corruption recovery — on `LoadAsync` failure, delete `index.bin`
   and fall through to fresh build (currently crashes on bad magic/version).
5. Refresh `_serviceAvailable` cache periodically (60s timer) so
   service-came-back transitions are detected.
6. Root-cause the NRE in `BaseLayoutPage.cs:620` (band-aided with
   `.Where(x => x is not null)`).
7. Surface service status (running, file count, indexing state, last
   update) in Settings UI.

**P2 — future scope**
- Token prefix matching (so `test` matches `testing` via tokens, not just
  trigrams).
- Pagination / cursor for >200 results.
- Memory budget tuning (1.2 GB for 1M files; trigram index dominates).
- Content search foundation prep (filename-only today).
- Library and Home scope fan-out to the indexed provider.
