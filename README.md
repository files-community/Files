# Files (search-rewrite fork)

Fork of [files-community/Files](https://github.com/files-community/Files)
exploring a faster search backend.

## What's different in this fork

A separate Rust process (`files-search-service.exe`) maintains a
Tantivy filename index over the user's home directory, with a
`ReadDirectoryChangesW` watcher and process throttling so it stays out
of the way. Files.App talks to it over gRPC via a new
`ISearchProvider` interface. The existing `Windows.Storage.Search`
path is preserved as the default provider; the new path is opt-in via
the `FILES_SEARCH_PROVIDER=Indexed` environment variable.

On a 5,000-file benchmark, the indexed provider answers substring
queries **~595× faster** than the legacy fallback path. Big O analysis
projects the gap to widen at larger scales (legacy is `O(N)` per
query when the path isn't in the Windows Search Indexer's catalog;
indexed is `O(log N)` always).

## Status

**Working PoC, seeking maintainer feedback before proposing PRs upstream.**

- ✅ Rust service: enumerator + Tantivy + watcher + throttling, 12 tests
- ✅ C# abstraction, legacy wrapper, indexed gRPC client
- ✅ Bench harness with JSON output
- ✅ Wired into Files.App via `SearchRouter`, default behavior unchanged
- ⏳ Service auto-launcher, content indexing, semantic search — gated
  on direction approval (see `docs/improvements.md`)

## Where to read

- **`docs/proposal.md`** — the pitch: what's the problem, what we built,
  bench numbers, what we're asking for. Start here if you're a maintainer.
- **`docs/improvements.md`** — concrete follow-ups, organized by tier
  with cost estimates. Designed to make it easy to say "yes to A, no
  to B" before we build anything.
- **`docs/search-roadmap.md`** — current state and what's next.
- **`docs/decisions/`** — ADRs for the technical choices.
- **`CLAUDE.md`** — the design constraints we held to.

## Trying it locally

```powershell
# Build the solution in VS 2026 (needs the v145 toolset; one upstream
# divergence in src/Files.App.Launcher noted in docs/decisions/).

# Build the Rust service:
cargo build --release --manifest-path src/search-service/Cargo.toml

# Set the opt-in env vars and start the service:
$env:FILES_SEARCH_PROVIDER = "Indexed"
$env:FILES_SEARCH_ROOT = "$env:USERPROFILE"
src/search-service/target/release/files-search-service.exe

# Launch Files.App from VS in a separate session.
```

Default users (no env var) get the existing search path, byte-identical
to upstream.

## Upstream

For everything else — features, bug reports, releases — see the
upstream repo: <https://github.com/files-community/Files>. This fork
is scoped to the search exploration; we don't carry other changes.
