#pragma once

std::string ExtractStringFromDLL(LPCWSTR dllName, int resourceIndex);
std::string IconToBase64String(HICON hIcon);
std::string IconToBase64String(HBITMAP hBitmap);
std::wstring GetDisplayName(IShellItem* iItem, SIGDN flags);
