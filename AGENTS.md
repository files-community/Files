# Files Development Guidelines

This project is a C#/.NET WinUI 3 desktop app; an alternative to File Explorer.

- Protect context usage. Any command with unknown or potentially large output must be capped. Prefer targeted commands such as `rg`, `Get-Content -TotalCount`, `Select-Object -First`, or focused `git diff -- <path>`; for example, `COMMAND 2>&1 | Select-Object -First 200`. If a line cap is still too noisy, narrow the query instead of dumping full output.
- Always follow `.editorconfig`
- Keep changed text files in CRLF line endings
- Keep comments concise and useful. Do not add comments that restate obvious code.
- Never read entire generated files in `bin` or `obj` unless the generated source is directly needed.
- Prefer targeted search over full file reads.
- Touch only what you must. Clean up only files you created or changed for the task.
- Treat file operations, shell integration, drag/drop, preview handlers, archive actions, settings persistence, and localization as high-risk areas.
- For Win32, COM, Shell, clipboard, hotkey, and file operation interop, prefer `src/Files.App.CsWin32`, `NativeMethods.txt`, and existing wrappers/helpers.
- Avoid ad hoc P/Invoke declarations when CsWin32 or existing interop code can cover the API.
- Do not edit generated CsWin32 output directly. Update source declarations, wrappers, or generator inputs instead.
- For UI work, use existing XAML resources, controls, converters, commands, and localization patterns. Avoid one-off styles or hard-coded user-visible strings.
- Start by identifying the smallest relevant project, feature area, and files for the task.
- Read nearby code before adding new abstractions. Prefer existing WinUI, MVVM, service, command, and storage patterns.
- Keep implementation scoped to the requested behavior. Avoid opportunistic refactors, formatting churn, dependency updates, and generated file edits.
- Treat tool output as evidence. When behavior changes, run the focused build that can prove it and report anything left unverified.

## Codebase Structure

```text
/src
├── Files.App                    Main WinUI app
├── Files.App.Controls           Shared app controls
├── Files.App.Storage            App storage abstractions and implementations
├── Files.App.CsWin32            Generated/native Win32 interop project
├── Files.App.BackgroundTasks    Background task project
├── Files.App.Server             App service/server project
├── Files.Core.SourceGenerator   Roslyn source generators and analyzers
├── Files.Core.Storage           Core storage abstractions
└── Files.Shared                 Shared attributes, extensions, and common code
```

```text
/tests
├── Files.App.UITests
├── Files.App.UnitTests
└── Files.InteractionTests
```

## Build

Prefer explicit platform/configuration builds.
Unless the task is specifically about resolving or inspecting warnings, add `-v:quiet -clp:ErrorsOnly` to `msbuild` commands so the log proves success or shows only actionable errors.

```powershell
msbuild -restore Files.slnx -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

If `msbuild` isn't available in the current shell, run it from Visual Studio Developer PowerShell. Match `-arch`, `-host_arch`, and `-p:Platform` to the platform you're verifying; use `x64` for x64 work and `arm64` for ARM64 work.

```powershell
pwsh.exe -NoProfile -Command "& {
  Import-Module 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll'
  Enter-VsDevShell 1ba2cc4e -SkipAutomaticLocation -DevCmdArguments '-arch=x64 -host_arch=x64'
  msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
}"
```

For focused C# work, build the affected project first.
Do not run build commands in parallel.

```powershell
msbuild -restore src/Files.Shared/Files.Shared.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.Core.SourceGenerator/Files.Core.SourceGenerator.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
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
- "Code Quality": anything else

The repository maintainers draft release notes based on these PR types: only fixes and feature requests are listed.

Good examples:

```text
Fix: Fixed an issue where thumbnails wouldn't refresh when a file was updated
Feature: Add support for previewing AVI files in the Preview Pane
Code Quality: Add source-generated settings serialization
```

For the PR body, follow `./.github/PULL_REQUEST_TEMPLATE.md`.
