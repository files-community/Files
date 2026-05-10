# CLAUDE.md

Fork of [files-community/Files](https://github.com/files-community/Files) (C#/WinUI 3). Goal: faster, AI-augmented search without regressing the rest.

## Search goals (hard constraints)

1. **Faster.** Query latency ≤10% of Windows Search on equivalent corpora.
2. **No heavier.** RAM/disk/idle CPU ≤ upstream Files + Windows Search Indexer.
3. **No user burden.** No UAC, no admin features, no new mandatory UI. Existing search bar only.

These are in tension; MFT-based indexing is disqualified (needs admin). Extract max speed within user-mode.

## Architecture

Separate Rust process, gRPC over named pipe to the C# UI. Keeps index out of GC, survives UI restarts.

- Enumeration: `FindFirstFileEx` + `FindExInfoBasic` + `FIND_FIRST_EX_LARGE_FETCH`, parallel work-stealing.
- Updates: `ReadDirectoryChangesW`, recursive, no polling.
- Throttle: `PROCESS_MODE_BACKGROUND_BEGIN`, pause on battery / fullscreen / high load.
- Indexes: Tantivy (filename + content), HNSW vectors (semantic), SQLite (tags/metadata).
- Query routing: glob/regex → filename; keywords → content; natural language → embeddings.

## Coexistence

All search goes through `ISearchProvider`. Two impls ship:

- `LegacySearchProvider` — wraps upstream unchanged. Frozen reference; instrumentation only.
- `IndexedSearchProvider` — talks to the new service.

Selected by setting → env var `FILES_SEARCH_PROVIDER` → default. Default stays `Legacy` until benchmarks pass.

## Layout

```
src/Files.App/                  UI, modified only to consume ISearchProvider
src/Files.SearchAbstraction/    interface + types
src/Files.LegacySearch/         upstream wrapper
src/Files.IndexedSearch.Client/ C# client
src/search-service/             Rust service
tests/Files.Search.Correctness/ result equivalence
tests/Files.Search.Bench/       perf benchmarks
tests/Files.Search.Resource/    soak + good-citizen tests
tests/corpora/                  deterministic corpus generators
```

## Tests

**Correctness.** For each `(corpus, query)`, indexed results ⊇ legacy results (modulo documented exclusions). Cases: exact, glob, substring, ext+substring, content, path-scoped, unicode, long paths, hidden/system/symlinks.

**Benchmarks.** Three corpora generated deterministically: `small` (50k files, ~2GB), `medium` (500k, ~50GB), `large` (2M, ~500GB). ~200 queries per corpus. Per `(provider, corpus, query)` record: time-to-first-result, time-to-complete, peak RAM, CPU-seconds, bytes read. Indexing also tracks: cold-start time, steady-state RAM, index size on disk, incremental update latency. JSON to `bench-results/<timestamp>.json`.

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