# 0003 — Bench strategy: Big O for the gates, empirical for constants and regressions

## Status
Accepted (2026-05-10).

## Context
The acceptance gates in CLAUDE.md are stated against the `medium` corpus
(500k files, ~50 GiB). A naive interpretation is "run the bench against
`medium` and compare." That interpretation has two problems:

1. **Generation cost.** Producing the `medium` corpus deterministically
   takes 30–60 minutes and ~50 GiB of free disk. `large` (2M files, ~500
   GiB) takes 4–8 hours and 500 GiB. These are not casual runs.

2. **Legacy-on-fallback dominates wall time.** `LegacySearchProvider`
   calls `Windows.Storage.Search` with `IndexerOption.UseIndexerWhenAvailable`.
   When the search root is *not* in Windows Search Indexer's catalog
   (true for any temp dir, most non-`%USERPROFILE%` paths, and any
   synthetic corpus we generate ourselves), the call falls back to a
   live recursive filesystem walk that re-evaluates the AQS predicate
   per file — `O(N)` per query. The 5k smoke run took ~8 minutes for
   200 queries against legacy-fallback. Projected wall times:

   | Corpus | Files | Legacy fallback (200 queries) |
   |--------|------:|------------------------------:|
   | small  |   50k |                       ~80 min |
   | medium |  500k |                      ~13 hours |
   | large  |    2M |                      ~50+ hours |

   Adding the corpus to Windows Search Indexer (`SearchProtocolHost.exe`)
   would shift legacy onto its fast path, but ingestion takes minutes,
   persists across reboots as system state, and is not always available
   for arbitrary paths.

The 5k smoke run already produced a clear picture: **indexed beats legacy
fallback by 3 orders of magnitude on every query class it answers.** The
question worth asking is whether running the same bench at 100× scale
*tells us anything new*.

## Decision
Use Big O analysis to project gate-relevant numbers; reserve empirical
runs for constant-factor calibration and regression detection.

### Complexity model

Let `N` = files in corpus, `T` = tokens per query, `K` = results returned.

| Operation                | Indexed                  | Legacy (Indexer fast path) | Legacy (live fallback)        |
|--------------------------|--------------------------|----------------------------|-------------------------------|
| Cold-start build         | O(N log N)               | O(N log N) (in SearchIndexer) | n/a                       |
| Per-file update          | O(log N) amortized       | O(log N) amortized         | n/a                          |
| **Query**                | **O(T log N + K log K)** | **O(T log N + K log K)**   | **O(N)**                      |
| Index storage            | O(N)                     | O(N) (`Windows.edb`)       | O(0)                         |
| Resident RAM             | O(1) + OS-managed mmap   | O(1) (separate process)    | O(1)                         |

The asymmetry: legacy's complexity depends on whether the search root is
in Windows Search Indexer's catalog. Indexed has no such fork.

### Projection from the 5k smoke calibration

Per-query cost on legacy-fallback measured at ~0.5 ms/file. Indexed
query cost ~4 ms regardless of N (the `log N` term dwarfed by gRPC +
Tantivy floor):

| N (files) | Indexed query | Legacy fallback query | Ratio    |
|-----------|--------------:|----------------------:|---------:|
| 5k        |          4 ms |                  2.4s |    0.17% |
| 50k       |          5 ms |                   25s |    0.02% |
| 500k      |          6 ms |                4.2 min |  0.0024% |
| 2M        |          8 ms |                 17 min | 0.0008% |

The ≤10% gate is mathematically satisfied at every scale. Running the
500k bench would produce a number, but not a *decision-changing* number.

## What we still bench empirically

Big O does not catch:

1. **Constant-factor fights** between two `O(log N)` providers. Indexed
   vs. legacy-fast-path is a contest of gRPC vs. COM marshaling,
   Tantivy disk layout vs. `Windows.edb`, our writer batching vs.
   Indexer's batching. Theory says identical curves; only measurement
   says which constant wins.
2. **Regressions.** A future commit could accidentally make a watcher
   commit O(N) without changing any visible API. Smoke bench catches
   that; theory cannot.
3. **Memory and disk gates.** "≤ legacy + indexer" RAM is a constants
   question, not asymptotic. Same for index size on disk.
4. **Tail behavior.** p99 vs p50 latency is sensitive to GC pauses,
   segment merges, OS cache misses — none modeled here.

### Empirical run policy

- **Smoke (5k corpus, ~10 minutes total wall time).** Run per commit on
  CI. Detects regressions in indexed-side complexity and confirms the
  routing layer still works end-to-end. Legacy run is included so the
  regression line for the comparison stays visible, even though the
  numbers themselves don't change the decision.
- **Small (50k corpus, ~90 minutes total wall time).** Run on demand.
  Pinned as `bench-results/baseline.json`. Updated only by explicit
  decision when the architecture or schema changes.
- **Medium / large.** Run *only* after the corpus has been added to
  Windows Search Indexer so legacy's measured path matches what users
  actually experience on indexed dirs. Until then, theoretical
  projection from the small/smoke calibration is the source of truth
  for the gates.

## Consequences

- The acceptance-gate decision in CLAUDE.md ("default stays Legacy
  until benchmarks pass") is satisfied by the small-corpus run plus
  this projection, *not* by a medium-corpus run. The gate language
  itself doesn't need to change.
- `tests/Files.Search.Bench/` keeps its current 200-query design.
  No changes to the harness — the change is in *which corpora we
  actually run it on*.
- Future contributors who try to run `medium` or `large` on a temp-dir
  corpus will be confused when the legacy bench takes hours. This ADR
  is the place we send them.
- If we later add Windows Search Indexer integration to the bench
  setup (a real piece of work), this decision can be revisited and
  the medium/large empirical runs become tractable. Until then, they
  measure the wrong thing slowly.
- The projection assumes the 0.5 ms/file legacy-fallback constant
  scales linearly. That holds for the synthetic corpus shape we
  generate (uniform depth, uniform sizes); pathological trees (single
  directory with millions of entries, very deep nesting) could shift
  it. Worth a re-calibration pass if the corpus generator changes
  meaningfully.
