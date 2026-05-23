# Files Development Guidelines

This project is a C#/.NET WinUI 3 desktop app; an alternative to File Explorer.

## Behavior

- Protect context usage. Any command with unknown or potentially large output must be byte-capped.
    Default pattern:
    ```bash
    COMMAND 2>&1 | head -c 4000
    ```
- Never read entire generated files (in the `bin`/`obj` directories) unless necessary; for example, unless needing to read the source-generated sources in the `obj` directory.
- Prefer targeted search over full file reads.
- Touch only what you must. Clean up only your own mess.

## Codebase Structure

```cmd
/src
  /Files.App                    Main WinUI app
  /Files.App.Controls           Shared app controls
  /Files.App.Storage            App storage abstractions and implementations
  /Files.App.CsWin32            Generated/native Win32 interop project
  /Files.App.BackgroundTasks    Background task project
  /Files.App.Server             App service/server project
  /Files.Core.SourceGenerator   Roslyn source generators and analyzers
  /Files.Core.Storage           Core storage abstractions
  /Files.Shared                 Shared attributes, extensions, and common code
```

```cmd
/tests
  /Files.App.UITests
  /Files.App.UnitTests
  /Files.InteractionTests
```

## Code Style

- Always follow `.editorconfig`
- Keep changed text files in CRLF line endings

## Build

Prefer explicit platform/configuration builds.

```powershell
msbuild -restore Files.slnx -p:Configuration=Debug -p:Platform=x64
```

For focused C# work, build the affected project first.
Do not run build commands in parallel.

```powershell
msbuild -restore src/Files.Shared/Files.Shared.csproj -p:Configuration=Debug -p:Platform=x64
msbuild -restore src/Files.Core.SourceGenerator/Files.Core.SourceGenerator.csproj -p:Configuration=Debug -p:Platform=x64
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64
```

## Test

We currently don't have a suitable set of tests for AI agents. Just make sure that the builds succeed.

## Commit & Push

When asked to commit, run these commands beforehand:

```powershell
git status --short
git diff --check
```

Do not revert unrelated user changes. Stage only files that belong to the requested change.

Use concise commit messages that describe the behavior change, for example:

```text
Add source-generated settings storage
```

## Open a PR

When asked to open a PR, use a short PR title that names the behavior, not the implementation mechanics only, and prepend the PR type:

- "Fix": use this prefix when the linked issue is a bug
- "Feature": use this prefix when the linked issue is a feature request
- "Code Quality": Anything else

The repository maintainer draft release notes based on these PR types: only fixes and feature requests are listed.

Good examples:

```text
Fix: Fixed an issue where thumbnails wouldn't refresh when a file was updated
Feature: Add support for previewing AVI files in the Preview Pane
Code Quality: Add source-generated settings serialization
```

For the PR body, follow `./.github/PULL_REQUEST_TEMPLATE.md`.
