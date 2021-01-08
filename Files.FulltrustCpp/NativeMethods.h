#pragma once

std::string ExtractStringFromDLL(LPCWSTR dllName, int resourceIndex);
std::string IconToBase64String(HICON hIcon);
std::string IconToBase64String(HBITMAP hBitmap);
std::wstring GetDisplayName(IShellFolder* psf, PITEMID_CHILD pidl, int flags);
std::wstring GetDisplayName(IShellItem2* iItem, SIGDN flags);
