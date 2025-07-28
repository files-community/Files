# Everything SDK3 (v1.5) Implementation

## Overview
This implementation adds support for Everything SDK3 (v1.5) with automatic fallback to SDK2 (v1.4).

## Features
- **SDK3 Support**: Uses Everything 1.5's new SDK3 for improved performance
- **Direct Folder Size Query**: SDK3's `Everything3_GetFolderSizeFromFilenameW()` provides instant folder sizes
- **Automatic Fallback**: Falls back to SDK2 if Everything 1.5 is not installed
- **Architecture Support**: Works with x86, x64, and ARM64

## Implementation Status
✅ SDK3 service implementation (`EverythingSdk3Service.cs`)
✅ Integration with main Everything service
✅ Folder size calculation optimization
✅ Search functionality with SDK3
✅ Graceful fallback handling

## Requirements
- Everything 1.5 alpha or later (for SDK3 features)
- SDK3 DLLs (not included - must be obtained separately)

## DLL Requirements
The SDK3 implementation requires the following DLLs:
- `Everything3.dll` (x86)
- `Everything3-x64.dll` (x64) 
- `Everything3-arm64.dll` (ARM64)

These DLLs are not included in the Files repository and must be obtained from:
https://github.com/voidtools/everything_sdk3

## Usage
The implementation automatically detects and uses SDK3 when available:
1. On startup, it attempts to connect to Everything 1.5
2. If successful, SDK3 features are used for improved performance
3. If not available, it falls back to SDK2 (Everything 1.4)

## Performance Benefits
- **Folder Size Calculation**: Near-instant with SDK3 vs enumeration with SDK2
- **Search Performance**: Improved query handling in SDK3
- **Memory Usage**: More efficient memory management in SDK3