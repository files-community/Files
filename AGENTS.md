# AGENTS.md

This is the primary repository guidance for AI coding agents. Keep `CLAUDE.md` and `.github/copilot-instructions.md` as thin entry points that refer back here instead of drifting into separate rule sets.

## Goal
- Submit small, issue-linked contributions that fit the current maintainer direction.
- Keep changes reviewable and avoid speculative product work.
- Do not mix unrelated fixes into the same PR.

## Approval and scope
- Work from a linked issue.
- Prefer issues marked `Ready to build`.
- If an issue is not marked `Ready to build`, treat a comment from a Files organization member as the minimum approval signal before implementing.
- Use one pull request per issue and link it in the PR body with `Closes #...`.
- Follow the repository PR template. Prefer `Fix:` or `Feature:` titles; if maintainers explicitly framed the linked issue as `Code Quality:`, mirroring that wording is acceptable.
- Always include concrete manual test steps, even for non-product or repo-hygiene changes.
- Do not add website-repo changes or unrelated `docs/` work from this repository unless the linked issue explicitly requires it.

## Repository map
- `Files.slnx`: main solution file.
- `src/Files.App`: main WinUI app and default startup project for local debugging.
- `src/Files.App.Server`: out-of-process COM server built before `Files.App` resolves assemblies.
- `src/Files.App.Storage`, `src/Files.Core.Storage`, `src/Files.Shared`: shared storage and runtime layers.
- `src/Files.Core.SourceGenerator`: Roslyn source generators and analyzers.
- `src/Files.App.CsWin32`: Win32 projections and `NativeMethods.txt`.
- `src/Files.App.OpenDialog`, `src/Files.App.SaveDialog`, `src/Files.App.Launcher`: native C++ helper projects; treat them as interop-sensitive.
- `tests/Files.InteractionTests`: packaged interaction tests run in CI.
- `tests/Files.App.UITests`: UITest host app and page coverage assets.

## Setup and validation
- Follow the official build guide: <https://files.community/docs/contributing/building-from-source>.
- Use Visual Studio 2022 17.13 or later with Windows 11 SDK `10.0.26100.0`, .NET SDK `10.0.102`, Windows App SDK `1.8`, MSVC v145 C++ tools, ATL, and Git for Windows.
- Open `Files.slnx`, set `Files.App` as the startup project, and use `Debug` with your local architecture for everyday development.
- For CI-aligned validation, inspect `.github/workflows/ci.yml`: XAML is checked with XAML Styler, the solution is restored and built across configurations, and `tests/Files.InteractionTests` runs from packaged `Release/x64` artifacts.
- Only run heavy packaging or interaction-test flows when the touched surface warrants it.

## Style
- Follow the official coding style guide: <https://files.community/docs/contributing/code-style>.
- Keep the codebase on the MVVM path and preserve existing architecture boundaries.
- Use XAML Styler for `.xaml` edits.
- Keep `.cs` and `.xaml` files tab-indented with a width of 4 and preserve CRLF line endings.
- Follow existing naming conventions: PascalCase for classes, methods, and properties; `I`-prefixed interfaces; `_camelCase` private fields; and `Async` suffixes for async methods.
- Put braces on new lines, avoid `#region`, and avoid broad style-only rewrites.
- UI changes should consider accessibility; use Accessibility Insights for Windows when the change touches keyboarding, focus, or visible controls.
- Prefer the smallest viable change and check for nearby occurrences of the same issue before submitting.

## Trimming and interop guardrails
- `src/Files.App.Server/Files.App.Server.csproj` enables `PublishTrimmed=true` outside `Debug`. Keep new code compatible with trimming.
- Avoid unconstrained reflection, runtime type scanning, serializer patterns that depend on unpreserved metadata, or ad-hoc dynamic loading without explicit justification.
- When adding Win32 or COM interop, prefer `src/Files.App.CsWin32` and `NativeMethods.txt` over new `DllImport` or `ComImport` declarations when practical.
- Be extra conservative when editing COM-server, source-generator, or native helper projects: small changes, explicit validation, and no incidental refactors.
