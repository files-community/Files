// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.WindowsAndMessaging;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate LRESULT WNDPROC(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
