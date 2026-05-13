# Files (search-rewrite fork)

Fork of [files-community/Files](https://github.com/files-community/Files)
exploring a faster search backend.

## What's different in this fork

A separate C# Windows Service (`files-search-service.exe`) maintains an
in-memory inverted + trigram filename index over the user's home directory,
with a `ReadDirectoryChangesW` watcher and process throttling so it stays
out of the way. Files.App talks to it over gRPC via a new `ISearchProvider`
interface. The existing `Windows.Storage.Search` path is preserved as the
default provider; the new path is opt-in via the **Use indexed search**
toggle in Settings → Advanced (or `FILES_SEARCH_PROVIDER=Indexed`).

On a 5,000-file benchmark, the indexed provider answers substring queries
**~595× faster** than the legacy fallback path. Big-O analysis projects the
gap to widen at larger scales (legacy is `O(N)` per query when the path
isn't in the Windows Search Indexer's catalog; indexed is `O(log N)` always).
See `docs/decisions/0003-bench-strategy-theoretical.md`.

## Status

**Working PoC on `feature/csharp-search-service`.**

- ✅ C# search service: USN enumerator + inverted/trigram index + watcher + throttling
- ✅ C# abstraction, legacy wrapper, indexed gRPC client over named pipe
- ✅ Bench harness with JSON output
- ✅ Wired into Files.App via `SearchRouter`, default behavior unchanged
- ✅ Settings UI toggle in Settings → Advanced
- ⏳ Packaged SCM end-to-end validation, content indexing — see
  `docs/search-roadmap.md`

## Where to read

- **`docs/csharp-search-service.md`** — full architecture: components, data
  flow, file map. Start here if you're a maintainer.
- **`docs/search-roadmap.md`** — current state and what's next.
- **`docs/decisions/`** — ADRs for the technical choices.
- **`CLAUDE.md`** — the design constraints we held to.

## Trying it locally

```powershell
# 1. Generate the small corpus (one-time, ~2 GB):
dotnet run --project tests\corpora -- --preset small --out .bench\small

# 2. Full bench: builds, starts the service, runs naive-scan + indexed,
#    gate-checks against bench-results/baseline.json:
.\run-bench.ps1

# Or run the service manually in dev console mode:
$env:FILES_SEARCH_ROOT      = ".bench\small"
$env:FILES_SEARCH_INDEX_DIR = ".bench\index"
dotnet run --project src\Files.SearchService -c Release

# Then launch Files.App from VS; set the toggle in Settings → Advanced,
# or override with $env:FILES_SEARCH_PROVIDER = "Indexed".
```

Default users (no toggle, no env var) get the existing search path,
byte-identical to upstream.

## Upstream

For everything else — features, bug reports, releases — see the
upstream repo: <https://github.com/files-community/Files>. This fork
is scoped to the search exploration; we don't carry other changes.
