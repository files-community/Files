# Discord post — for the user to copy / adapt before sending

Not committed to a public-facing surface. This is the conversational
version of `docs/proposal.md` that fits a Discord channel.

---

## Short version (~6 lines, fits one chat message)

> Hey — I've been working on a fork that swaps the
> Windows.Storage.Search backend for a sidecar Rust indexer (Tantivy
> + ReadDirectoryChangesW). On a 5k-file bench it's ~595× faster on
> substring queries; default is unchanged, indexed is opt-in via env
> var. Wanted to ask the team: would this direction be of interest
> upstream? Don't want to keep building if it's a non-starter.
>
> Repo + writeup: <link to your fork>
> Specifically the proposal: <link to docs/proposal.md on GitHub>

## Longer version (if a maintainer engages)

> A bit more context on what's in the fork:
>
> **What it is:** separate Rust process, gRPC over TCP (named pipe is
> next). Tantivy for the filename index, FindFirstFileExW + rayon for
> enumeration, notify crate (ReadDirectoryChangesW) for live updates,
> SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN) + pause-on-battery /
> fullscreen / load for being a good citizen.
>
> **What it isn't yet:** content indexing, semantic search, named-pipe
> transport, or service auto-launcher. Those are bounded but real
> work — `docs/improvements.md` has them tiered with cost estimates.
> Holding off on building more until I get a read on whether the
> direction is even welcome.
>
> **What I'd want your read on:**
> 1. Is a sidecar Rust process inside a C# app something you'd accept
>    in principle?
> 2. What would block it — Rust toolchain in CI, signing, maintenance
>    burden, telemetry concerns?
> 3. Phased PRs (interface → bench harness → indexed client → router)
>    or stay-as-fork preferred?
>
> Happy to walk through any of it on a call or via PR comments. No
> hard feelings if the answer is "stay a fork" — just want to know
> before sinking another week into it.

## Notes on framing

- Lead with the question, not the code dump. Maintainers who skim
  Discord see "would this be of interest" first; the link is for if
  they want to dig.
- Include the bench number — it's the hook. "595× faster on substring"
  is concrete enough to make someone click.
- Soft-close ("no hard feelings if…") signals you're not emotionally
  invested in a yes; lowers the stakes of their reply.
- Don't mention "I used Claude Code" or AI-assisted in the pitch.
  Maintainers care about the code and architecture, not the toolchain
  behind it. If asked directly, be honest, but don't lead with it.

## After you post

Things to be ready to answer fast:

- "Why Rust and not C# / .NET out-of-process?" — Tantivy maturity,
  zero-GC for the index, single-binary distribution. ADR-grade
  answer in `docs/decisions/`.
- "Why a sidecar process and not in-proc?" — index outlives UI
  crashes, GC isolation, can be restarted independently. Architecture
  in `CLAUDE.md`.
- "How does the indexer affect privacy / telemetry?" — index is
  local-only, in `%LOCALAPPDATA%\Files\search-index\`. No network,
  no upload. Worth saying explicitly.
- "What about admin / MFT for max speed?" — explicit no per CLAUDE.md
  goal #3; a future opt-in "Turbo Mode" is on the table. Don't
  oversell it.
- "Does it work on Windows on ARM?" — Rust cross-compiles fine; we
  haven't tested the ARM path. Honest "untested but no architectural
  blockers."
