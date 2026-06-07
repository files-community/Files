# Files - Linux Port Guide

This document describes the Linux port of the Files file manager using Avalonia and .NET.

## Architecture

### Project Structure

```
/src
├── Files.Linux.UI              # Avalonia-based UI layer
│   ├── Views/                  # XAML/AXAML UI components
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Models/                 # Data models
│   └── App.axaml(.cs)          # Application entry point
│
├── Files.Linux.Platform         # Linux-specific platform layer
│   ├── Storage/                # Linux file system access
│   ├── Desktop/                # Desktop environment integration
│   ├── Mime/                   # MIME type resolution
│   └── Utils/                  # Linux utility functions
│
├── Files.Core.Storage          # Shared storage abstractions (cross-platform)
├── Files.Shared                # Shared models and helpers
└── ...                         # Other core projects
```

### Platform Layer

The **Files.Linux.Platform** project provides:

- **LinuxStorageProvider**: File system operations (list, create, delete, copy, rename)
- **DesktopIntegration**: Linux desktop environment support (xdg-open, clipboard, themes)
- **MimeTypeResolver**: MIME type detection for files
- **LinuxPaths**: XDG directory standard utilities

### UI Layer

The **Files.Linux.UI** project uses **Avalonia**, a cross-platform WinUI-inspired framework:

- AXAML markup for UI (similar to WPF/WinUI)
- Fluent theme for consistent appearance
- MVVM pattern with ViewModels
- DataBinding for reactive UI updates

## Building the Linux Version

### Prerequisites

- **.NET 8 SDK** or later
- **Linux distribution** (tested on Ubuntu 22.04+, Fedora 38+, etc.)
- **Avalonia development tools**

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build for x64
dotnet build -c Release -p:Platform=x64 src/Files.Linux.UI/Files.Linux.UI.csproj

# Build for arm64 (Raspberry Pi, etc.)
dotnet build -c Release -p:Platform=arm64 src/Files.Linux.UI/Files.Linux.UI.csproj

# Run directly
dotnet run --project src/Files.Linux.UI/Files.Linux.UI.csproj
```

### Creating a Native Package

#### AppImage (works on most distros)

```bash
# Build release
dotnet publish -c Release -p:Platform=x64 -o output/linux-x64

# Create AppImage using appimagetic or similar tools
```

#### Snap Package

```bash
# Install snapcraft
sudo apt install snapcraft

# Create snapcraft.yaml in root
# Then run: snapcraft
```

#### Flatpak

```bash
# Install flatpak tools
sudo apt install flatpak-builder

# Create flatpak manifest
# Then build with flatpak-builder
```

#### System Packages

Create .deb, .rpm, or other native packages using your distro's packaging tools.

## Desktop Integration Features

### XDG Standards Support

- ✅ XDG Base Directory specification (~/.config, ~/.local/share, ~/.cache)
- ✅ XDG MIME application handling (xdg-open)
- ✅ Desktop environment detection
- ✅ Theme preference detection (light/dark mode)

### Supported Desktop Environments

- GNOME (primary target)
- KDE Plasma
- XFCE
- Cinnamon
- Budgie
- MATE
- Others following freedesktop.org standards

## Keyboard Shortcuts (Linux Convention)

| Action | Shortcut |
|--------|----------|
| Open/Launch | Enter |
| Back | Alt+Left |
| Forward | Alt+Right |
| Go to Parent | Alt+Up |
| Refresh | F5 / Ctrl+R |
| Cut | Ctrl+X |
| Copy | Ctrl+C |
| Paste | Ctrl+V |
| Delete | Delete |
| Rename | F2 |
| New Folder | Ctrl+Shift+N |
| Select All | Ctrl+A |
| Deselect | Ctrl+Shift+A |
| Search | Ctrl+F |
| Preferences | Ctrl+, |
| Quit | Ctrl+Q |

## Platform-Specific Behaviors

### File Permissions

- Linux files can be executable. The app shows executable badges on shell scripts and binaries.
- Handles read-only files appropriately.
- Respects file ownership and permissions from the filesystem.

### Hidden Files

- Files starting with `.` are hidden by default (Linux convention).
- Visible in UI through View → Show Hidden Files.

### Path Handling

- Uses `/` as path separator (normalized from Windows `\`).
- Root directory is `/`, not `C:\`.
- Home directory is `~` or `$HOME`.

### Symlinks

- Basic symlink support to be added.
- Shows symlink targets in file properties.

## Known Limitations & TODOs

- [ ] Symlink display and handling
- [ ] Advanced file permissions UI
- [ ] Thumbnail generation for images/videos
- [ ] File search (find, locate integration)
- [ ] Terminal integration (open terminal here)
- [ ] Network locations (NFS, SMB/CIFS)
- [ ] Trash/Recycle bin (freedesktop.org Trash spec)
- [ ] File context menu (right-click)
- [ ] Drag and drop
- [ ] File properties dialog
- [ ] Bookmarks/Favorites sidebar

## Troubleshooting

### Build Issues

**Problem**: `dotnet restore` fails
- **Solution**: Update .NET SDK: `dotnet --version` should show 8.0+

**Problem**: Avalonia not found
- **Solution**: Check `Directory.Packages.props` for correct version and source

### Runtime Issues

**Problem**: Window doesn't appear
- **Solution**: Check `$XDG_RUNTIME_DIR` and wayland/X11 session

**Problem**: File permissions denied
- **Solution**: Check Linux file permissions with `ls -la`

**Problem**: xdg-open not working
- **Solution**: Install `xdg-utils`: `sudo apt install xdg-utils` (Debian/Ubuntu)

## Contributing

When porting Windows-specific code:

1. Create abstractions in `Files.Core.Storage`
2. Implement platform-specific code in `Files.Linux.Platform`
3. Use interfaces to keep UI code platform-agnostic
4. Test on multiple desktop environments

## References

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [XDG Base Directory Spec](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html)
- [freedesktop.org Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html)
- [Linux File Hierarchy Standard](https://refspecs.linuxfoundation.org/FHS_3.0/fhs-3.0.html)