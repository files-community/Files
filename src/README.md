<!--  Copyright (c) 2024 Files Community. Licensed under the MIT License. See the LICENSE.  -->

# Files Source Code

## Our goals

Please refer to https://github.com/files-community/Files/issues/4180 for our goals.
We are actively working on refactoring, optimizing and improving performance and codebase.

- Refactoring codebase
- Less than 1% crash rate
- Robust file system operations
- Robust file system accessing
- Higher storage thumbnail quality
- Performance on multitasking

## Projects

Name|Language|Built with|Description
---|---|---|---
Files.App.Package<br/>Files.App (Package)|*None*|[WinAppSdk](https://learn.microsoft.com/windows/apps/windows-app-sdk)|Packaging project with [WAP](https://learn.microsoft.com/windows/apps/get-started/intro-pack-dep-proc) for `Files.App` project on Windows.
Files.App.BackgroundTasks|C#|[CsWinRT](https://learn.microsoft.com/windows/apps/develop/platform/csharp-winrt)|In-proc background service on Windows.
Files.App.Launcher|C++|[Win32 API](https://learn.microsoft.com/windows/win32/api)|Entry point of a process to override from `Win+E` or `explorer.exe` to launch Files via Windows Registry on Windows.
Files.App.OpenDialog|C++|[Win32 API](https://learn.microsoft.com/windows/win32/api)|Entry point of a process to override `FileOpenDialog` common dialog on Windows.
Files.App.SaveDialog|C++|[Win32 API](https://learn.microsoft.com/windows/win32/api)|Entry point of a process to override `FileSaveDialog` common dialog on Windows.
Files.App.Server|C#|[CsWinRT](https://learn.microsoft.com/windows/apps/develop/platform/csharp-winrt)|Out-of-proc background service to safely continue ongoing tasks even after foreground processes are terminated. This is interoperable between the server process and Files processes using C#/WinRT projection because it's supposed to be shared by multiple Files processes.
Files.App.Storage|C#|*None*|Implementation of Files Storage Layer on Windows.
Files.App|C#|[WinAppSdk](https://learn.microsoft.com/windows/apps/windows-app-sdk)|Entry point and UI thread of Files on Windows.
Files.Core.SourceGenerator|C#|*None*|Source generators to boost developer experience for Files.
Files.Core.Storage|C#|*None*|Interfaces of Files Storage Layer.
Files.Shared|C#|*None*|Fundamental helpers and extensions.
