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
		swprintf(debugPath, _countof(debugPath) - 1, L"%s\\%s", pszPath, L"open_in_folder.txt");
		//_wfreopen_s(&_debugStream, debugPath, L"w", stdout);
		CoTaskMemFree(pszPath);
	}

	//Sleep(10 * 1000); // Uncomment to attach debugger

	int numArgs = 0;
	bool withArgs = false;
	LPWSTR* szArglist = CommandLineToArgvW(GetCommandLine(), &numArgs);
	WCHAR openDirectory[MAX_PATH];

	if (numArgs > 1)
	{
		swprintf(openDirectory, _countof(openDirectory) - 1, L"%s", szArglist[1]);
		std::wcout << openDirectory << std::endl;
		withArgs = true;
	}

	LocalFree(szArglist);

	WCHAR szBuf[MAX_PATH];
	ExpandEnvironmentStrings(L"%LOCALAPPDATA%\\Microsoft\\WindowsApps\\files.exe", szBuf, MAX_PATH - 1);
	std::wcout << szBuf << std::endl;
	if (_waccess(szBuf, 0) == -1)
	{
		std::cout << "Files has been uninstalled" << std::endl;

		MessageBox(
			NULL,
			(LPCWSTR)L"Files has been uninstalled. Restoring File Explorer.",
			(LPCWSTR)L"Files",
			(UINT)(MB_OK)
		);

		// Uninstall launcher
		TCHAR szFile[MAX_PATH], szCmd[MAX_PATH];
		swprintf(szCmd, _countof(szCmd) - 1, L"/c reg.exe import \"%s\"", L"%LocalAppData%\\Files\\UnsetFilesAsDefault.reg");
		if (((INT)ShellExecute(0, L"runas", L"cmd.exe", szCmd, 0, SW_HIDE) > 32))
		{
			std::cout << "Launcher unset as default" << std::endl;
			swprintf(szCmd, _countof(szCmd) - 1, L"-command \"Start-Sleep -Seconds 5; $lfp = [System.Environment]::ExpandEnvironmentVariables('%%LocalAppData%%\\Files'); Remove-Item -Path $lfp -Recurse -Force\"");
			if ((INT)ShellExecute(0, 0, L"powershell.exe", szCmd, 0, SW_HIDE) > 32)
			{
				std::cout << "Launcher uninstalled" << std::endl;
			}
		}

		// Run explorer
		SHELLEXECUTEINFO ShExecInfo = { 0 };
		ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
		ShExecInfo.lpFile = L"explorer.exe";
		if (withArgs)
		{
			TCHAR args[1024];
			swprintf(args, _countof(args) - 1, L"\"%s\"", openDirectory);
			ShExecInfo.lpParameters = args;
		}
		ShExecInfo.nShow = SW_SHOW;
		ShellExecuteEx(&ShExecInfo);

		if (_debugStream)
		{
			fclose(_debugStream);
		}

		return 0;
	}

	if (withArgs)
	{
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
			if (_debugStream)
			{
				fclose(_debugStream);
			}
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
			swprintf(args, _countof(args) - 1, L"-directory \"%s\"", openDirectory);
		}
		else
		{
			std::wcout << L"Item: " << item << std::endl;
			swprintf(args, _countof(args) - 1, L"-select \"%s\"", item.c_str());
		}

		std::wcout << L"Invoking: " << args << std::endl;
		ShExecInfo.lpParameters = args;
		ShExecInfo.nShow = SW_SHOW;
		ShellExecuteEx(&ShExecInfo);
	}
	else
	{
		SHELLEXECUTEINFO ShExecInfo = { 0 };
		ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
		ShExecInfo.lpFile = L"files.exe";
		std::wcout << L"Invoking: no arguments" << std::endl;
		ShExecInfo.nShow = SW_SHOW;
		ShellExecuteEx(&ShExecInfo);
	}

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