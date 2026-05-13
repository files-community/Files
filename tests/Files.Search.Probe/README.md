# Files.Search.Probe

Integration harness for `Files.SearchService`. Exercises the real gRPC client
(`Files.IndexedSearch.Client`) against the running service over TCP, so search
behavior can be verified end-to-end without launching the WinUI app.

## Usage

```
dotnet run --project tests/Files.Search.Probe          # full 7-check suite
dotnet run --project tests/Files.Search.Probe -- query "readme"   # ad-hoc query, shows scores
dotnet run --project tests/Files.Search.Probe -- bench            # latency table across 8 common terms
```

The probe auto-starts `files-search-service.exe` if no instance is running. It
expects the service binary at the path defined by `ServiceExe` in `Program.cs`
(default: the project's `bin/x64/Debug/.../files-search-service.exe`).

## What the suite checks

| Test | Verifies |
|---|---|
| service is up | gRPC reachable; `IndexedFileCount > 1000` |
| scoped search <500ms | search inside `UserProfile`, returns results, under deadline |
| Home/unscoped search <500ms | empty scope path = search whole index |
| trigram substring | mid-string match for queries ≥3 chars |
| nonexistent query | unmatched query returns 0 fast |
| no CPU pinning | service uses <600% CPU-of-wall during a 30 ms query burst |
| warm channel <100ms | second query through the same provider is fast |

## When to use vs MSTest projects

- `Files.Search.Correctness` — unit tests on `FileIndex`/`Tokenizer`/`Scorer`/`IndexPersistence`. In-process, no service.
- `Files.Search.Bench` — perf benchmarks against the legacy provider for the CLAUDE.md gates.
- `Files.Search.Probe` (this) — end-to-end integration over the real gRPC transport. Useful for iterating on routing, transport, and lifecycle without rebuilding Files.App.
