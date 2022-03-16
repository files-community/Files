//

#include <iostream>

#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "Propsys.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "uuid.lib")

#include <wil/resource.h>

#include "OpenInFolder.h"

#define ID_TIMEREXPIRED 101

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

int WINAPI WinMain(HINSTANCE hInstance,
	HINSTANCE hPrevInstance,
	LPSTR     lpCmdLine,
	int       cmdShow)
{
	auto oleCleanup = wil::OleInitialize_failfast();

	FILE* _debugStream = NULL;
	PWSTR pszPath = NULL;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &pszPath);
	if (SUCCEEDED(hr))
	{
		TCHAR debugPath[MAX_PATH];
		wsprintf(debugPath, L"%s\\%s", pszPath, L"open_in_folder.txt");
		//_wfreopen_s(&_debugStream, debugPath, L"w", stdout);
		CoTaskMemFree(pszPath);
	}

	//Sleep(10 * 1000);

	int numArgs = 0;
	LPWSTR* szArglist = CommandLineToArgvW(GetCommandLine(), &numArgs);
	WCHAR openDirectory[MAX_PATH];

	if (numArgs < 2)
	{
		LocalFree(szArglist);
		if (_debugStream)
		{
			fclose(_debugStream);
		}
		return -1;
	}

	wsprintf(openDirectory, L"%s", szArglist[1]);

	LocalFree(szArglist);

	std::wcout << openDirectory << std::endl;

	WCHAR szBuf[MAX_PATH];
	ExpandEnvironmentStrings(L"%LOCALAPPDATA%\\Microsoft\\WindowsApps\\files.exe", szBuf, MAX_PATH - 1);
	std::wcout << szBuf << std::endl;
	if (_waccess(szBuf, 0) == -1)
	{
		std::cout << "Files has been uninstalled" << std::endl;
		// TODO: run explorer and uninstall launcher
		SHELLEXECUTEINFO ShExecInfo = { 0 };
		ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
		ShExecInfo.lpFile = L"explorer.exe";
		TCHAR args[1024];
		wsprintf(args, L"\"%s\"", openDirectory);
		ShExecInfo.lpParameters = args;
		ShExecInfo.nShow = SW_SHOW;
		ShellExecuteEx(&ShExecInfo);
		return -1;
	}

	// Register the window class.
	const wchar_t CLASS_NAME[] = L"Files Window Class";

	WNDCLASSEX wcex = { };
	wcex.cbSize = sizeof(wcex);
	wcex.lpfnWndProc = WindowProc;
	wcex.cbWndExtra = sizeof(OpenInFolder*);
	wcex.hInstance = hInstance;
	wcex.lpszClassName = CLASS_NAME;
	RegisterClassEx(&wcex);

	OpenInFolder openInFolder;

	// Create the window.
	HWND hwnd = CreateWindowEx(
		0,                         // Optional window styles.
		CLASS_NAME,                // Window class
		L"Files Launcher",         // Window text
		0,                         // Window style
		0, 0, 0, 0,    // Size and position
		HWND_MESSAGE,  // Parent window (message-only window)
		NULL,          // Menu
		hInstance,     // Instance handle
		&openInFolder  // Additional application data
	);

	if (hwnd == NULL)
	{
		return 0;
	}

	SetTimer(hwnd, ID_TIMEREXPIRED, 500, NULL);

	ShowWindow(hwnd, SW_SHOWNORMAL);
	//UpdateWindow(hwnd);

	MSG msg = { };
	while (GetMessage(&msg, NULL, 0, 0) > 0)
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	auto item = openInFolder.GetResult();

	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.lpFile = L"files.exe";
	
	TCHAR args[1024];
	if (item.empty())
	{
		std::wcout << L"No item selected" << std::endl;
		wsprintf(args, L"-directory \"%s\"", openDirectory);
	}
	else
	{
		std::wcout << L"Item: " << item << std::endl;		
		wsprintf(args, L"-select \"%s\"", item.c_str());
	}

	std::wcout << L"Invoking: " << args << std::endl;
	ShExecInfo.lpParameters = args;
	ShExecInfo.nShow = SW_SHOW;
	ShellExecuteEx(&ShExecInfo);

	if (_debugStream)
	{
		fclose(_debugStream);
	}

	return 0;
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	auto* pContainer = (OpenInFolder*)GetWindowLongPtr(hwnd, GWLP_USERDATA);

	switch (uMsg)
	{
	case WM_NCCREATE:
	{
		CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
		pContainer = reinterpret_cast<OpenInFolder*>(pCreate->lpCreateParams);

		if (!pContainer)
		{
			PostQuitMessage(0);
			return 0;
		}

		pContainer->SetWindow(hwnd);
		SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)pContainer);
		break;
	}

	case WM_NCDESTROY:
		SetWindowLongPtr(hwnd, GWLP_USERDATA, 0);
		return 0;

	case WM_TIMER:
		switch (wParam)
		{
		case ID_TIMEREXPIRED:
			PostQuitMessage(0);
			return 0;
		}
		break;
	}

	/* Jump across to the member window function (will handle all requests). */
	if (pContainer != nullptr)
	{
		return pContainer->WindowProcedure(hwnd, uMsg, wParam, lParam);
	}
	else
	{
		return DefWindowProc(hwnd, uMsg, wParam, lParam);
	}
}