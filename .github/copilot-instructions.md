# Copilot instructions for Files

Follow [../AGENTS.md](../AGENTS.md) for repository-specific guidance.

Key reminders:
- Work from one linked issue and keep the pull request surface narrow.
- `Files.slnx` is the main solution, and `src/Files.App` is the default startup project for local debugging.
- Follow MVVM, existing naming and brace conventions, and XAML Styler for `.xaml` files.
- Preserve CRLF line endings. `.cs` and `.xaml` files use tab indentation.
- `Files.App.Server` is trimmed in Release builds, so avoid trim-hostile runtime patterns.
- Prefer `src/Files.App.CsWin32` for new Win32 or COM bindings.
