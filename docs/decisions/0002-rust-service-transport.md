# 0002 — Rust service transport: TCP for v0, named pipe later

## Status
Accepted (2026-05-09).

## Context
CLAUDE.md commits the search service to gRPC over named pipe. The named pipe
choice is right long-term (no firewall prompts, OS-level ACLs, no port
collisions, can't be reached from off-box), but tonic does not ship a
Windows-named-pipe transport — it requires a custom `Connector` and
`Acceptor` wrapping `tokio::net::windows::named_pipe`, plus matching code in
the C# client.

The service has nothing to serve yet: no index, no enumerator, no watcher.
Spending the first day getting a non-trivial transport working trades real
progress for plumbing.

## Decision
Bind to `127.0.0.1:50080` for v0. Swap to `\\.\pipe\files-search` once the
service is doing enough to be worth integration-testing from the C# client
— concretely, when an in-memory filename index returns hits for a hard-coded
corpus.

## Consequences
- v0 is reachable from any process on the box. Acceptable: no real data is
  served yet, and the service will not auto-start until the transport is
  hardened.
- The transport swap is local to `main.rs` and the C# client connection
  setup; no proto or service-trait changes.
- Revisit before any acceptance-gate benchmark run — TCP loopback adds
  measurable per-call overhead vs. named pipe and would skew
  time-to-first-result.
