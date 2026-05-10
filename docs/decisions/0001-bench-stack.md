# 0001 — Bench harness & corpus generator stack

**Date:** 2026-05-09
**Status:** Accepted

## Decision

Both the corpus generator (`tests/corpora/`) and the bench harness (`tests/Files.Search.Bench/`) are .NET 10 console apps in C#. The bench harness exercises the **same Windows APIs** the legacy `FolderSearch` uses — `StorageFolder.CreateItemQueryWithOptions` with AQS — rather than instantiating `FolderSearch` itself.

## Why

- Matches repo toolchain (.NET 10, already in `global.json`); no extra build infra.
- The legacy perf characteristic we are racing is the Windows Search Indexer + AQS pipeline. `FolderSearch` is a thin async wrapper around it; results are equivalent for benchmarking purposes.
- `FolderSearch` is heavily coupled to the Files.App runtime (`Ioc.Default`, `App.LibraryManager`, `IUserSettingsService`, etc.). Hosting it standalone would mean booting half the WinUI app or refactoring it first — neither belongs on the critical path of "establish a baseline."
- Keeps the harness reproducible from CI without a UI session.

## Rejected

- **Rust harness.** Adds toolchain before we need it; the search-service project will have its own Rust crate later.
- **Hosting Files.App in-process.** Couples the bench to UI startup and IoC; flaky and slow.
- **BenchmarkDotNet.** Designed for microbenchmarks; our metrics (peak RAM, CPU-seconds, bytes read, time-to-first-result on 200 queries) need bespoke instrumentation anyway.

## Output schema

Each run writes `bench-results/<ISO8601>.json`:

```jsonc
{
  "schemaVersion": 1,
  "runId": "2026-05-09T12-34-56Z",
  "machine": { "os": "...", "cpu": "...", "ramGB": 32, "diskKind": "NVMe" },
  "provider": "legacy" | "indexed" | "turbo",
  "corpus": { "name": "small", "files": 50000, "bytes": 2147483648, "seed": 42 },
  "indexing": {
    "coldStartMs": 0,
    "steadyStateRamMB": 0,
    "indexBytesOnDisk": 0,
    "incrementalUpdateP95Ms": 0
  },
  "queries": [
    {
      "id": "ext-docx",
      "text": "*.docx",
      "class": "glob",
      "timeToFirstResultMs": 0,
      "timeToCompleteMs": 0,
      "resultCount": 0,
      "peakRamMB": 0,
      "cpuSeconds": 0,
      "bytesRead": 0
    }
  ]
}
```

`baseline.json` is a copy of one chosen run, updated only by explicit decision (per CLAUDE.md).

## Query classes (~200 total per corpus)

`exact`, `glob`, `substring`, `ext+substring`, `content`, `path-scoped`, `unicode`, `long-path`, `hidden-system-symlink`. Same set used by the correctness suite, so a single `queries.json` feeds both.
