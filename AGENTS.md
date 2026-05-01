# Repository Instructions

I'm a C# WinUI developer. Answer in Japanese. May ask about C++.

## When to Deal With Interop Code

When the user asks to convert marshaled interop code into unmarshaled interop, or asks to remove trim-unsafe manual P/Invoke definitions, see [docs/interop-unmarshaled-conversion.md](docs/interop-unmarshaled-conversion.md).

Prefer adding APIs and related generated types to `src/Files.App.CsWin32/NativeMethods.txt`, then update the callees to use CsWin32-generated `Windows.Win32.PInvoke` APIs directly. Do not leave manual `DllImport` definitions in place or replace them with local `LibraryImport` declarations when CsWin32 can generate the API.
