# Files Developer Documentation

This folder contains documentation for people and AI agents working in the
Files repository. Docs are organized by purpose so readers can tell whether a
page describes the code that exists today, the architecture we are moving
toward, the migration path, or the reason behind a decision.

## Sections

- [current](current/README.md): current implementation notes and code-reading
  maps.
- [architecture](architecture/README.md): current architecture maps plus target
  architecture and adopted abstractions.
- [migration](migration/README.md): plans and checklists for moving from the
  current implementation to the target architecture.
- [decisions](decisions/README.md): ADRs that explain why a direction was
  chosen.

## Where to add docs

| If the doc explains... | Put it in... |
| --- | --- |
| How a feature is implemented today, including services, Win32 APIs, COM interfaces, or libraries | `current` |
| The abstraction or design we want new code to follow | `architecture` |
| A staged plan to replace or reshape existing code | `migration` |
| Why one approach was chosen over another | `decisions` |
