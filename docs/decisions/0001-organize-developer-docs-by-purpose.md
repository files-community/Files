# ADR 0001: Organize Developer Docs by Purpose

## Status

Accepted

## Context

The developer docs mix several kinds of information: current implementation
notes, target architecture, migration guidance, and decision rationale. Keeping
these in one flat folder makes it harder to tell whether a document describes
the code as it exists today or the design that new code should move toward.

## Decision

Organize docs under four purpose-based folders:

- `current`: current implementation notes and code-reading maps.
- `architecture`: adopted architecture and target abstractions.
- `migration`: plans for moving current code toward the target architecture.
- `decisions`: ADRs explaining why important choices were made.

## Consequences

Readers can choose the right section based on intent. Storage Abstraction specs
live under `architecture`, while native interop conversion guidance lives under
`migration`. Feature implementation notes should be added under `current` until
they describe an adopted target design.
