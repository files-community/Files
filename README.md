<p align="center">
  <img alt="Files Logo" src="src/Files.App%20(Package)/Assets/AppTiles/Release/StoreLogo.scale-400.png" width="100px" />
  <h1 align="center">Files</h1>
</p>

[![Build Status](https://dev.azure.com/filescommunity/Files/_apis/build/status/Build%20Pipeline?branchName=main)](https://dev.azure.com/filescommunity/Files/_build/latest?definitionId=4&branchName=main)
[![Crowdin](https://badges.crowdin.net/files-app/localized.svg)](https://crowdin.com/project/files-app)
[![Discord](https://discordapp.com/api/guilds/725513575971684472/widget.png)](https://discord.gg/files)
<a style="text-decoration:none" href="https://www.microsoft.com/store/apps/9NGHP3DX8HDX">
    <img src="https://img.shields.io/badge/Microsoft%20Store-Download-purple.svg?style=flat-round" alt="Store link" />
</a>
<a style="text-decoration:none" href="https://files.community/download/stable">
    <img src="https://img.shields.io/badge/Sideload-Download-purple.svg?style=flat-round" alt="Sideload link" />
</a>
<a style="text-decoration:none" href="https://files.community/download/preview">
    <img src="https://img.shields.io/badge/Preview-Download-blue.svg?style=flat-round" alt="Preview link" />
</a>

Files is a file manager that lets you easily organize content on your device. Robust multitasking experiences, helpful tags, and deep integrations add to an intuitive design – openly developed right here.

We welcome feedback items and approved community contributions! Vague ideas are difficult to act on, so you'll need to fill out the correct issue template with detailed information such as related links or screenshots. Keep discussions constructive by reading our [code of conduct](https://github.com/files-community/Files/blob/main/CODE_OF_CONDUCT.md).

## Privacy
This project uses App Center to drive and inform quality improvements. We may collect anonymous information not limited to the settings in use and crash reports. All information sent is free of any user-identifying or contextual data.

## FAQ
Have any questions? Check out our [documentation](https://files.community/docs)!

## Building from source

### 1. Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the following individual components:
    - Windows 11 SDK (10.0.22621.0)
    - .NET 7 SDK
    - MSVC v143 - VS 2022 C++ x64/x86 or ARM64 build tools (latest)
    - C++ ATL for latest v143 build tools (x86 & x64 or ARM64)
    - Git for Windows
- [Windows App SDK 1.2](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads#current-releases)
    
### 2. Clone the repository

```ps
git clone https://github.com/files-community/Files
```

This will create a local copy of the repository.

### 3. Build the project

To build Files for development, open the `Files.sln` item in Visual Studio. Right-click on the `Files.Package` packaging project in solution explorer and select ‘Set as Startup item’.

In the top pane, select the items which correspond to your desired build mode and the processor architecture of your device like below:
![image](https://user-images.githubusercontent.com/39923744/148721296-2bd132d0-4a4d-4555-8f58-16b00b18ade3.png)

## Contributors

Want to contribute to this project? Let us know with an [issue](https://github.com/files-community/Files/issues) that communicates your intent to create a [pull request](https://github.com/files-community/Files/pulls). Also, view our [contributing guidelines](https://github.com/files-community/Files/blob/main/.github/CONTRIBUTING.md) to make sure you're up to date on the coding conventions.

## Screenshots

![Files](src/Files.App/Assets/FilesHome.png)
