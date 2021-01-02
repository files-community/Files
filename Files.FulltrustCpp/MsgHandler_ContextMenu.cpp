#include "pch.h"
#include "MsgHandler_ContextMenu.h"

LRESULT CALLBACK WndProc(HWND hwnd, UINT uiMsg, WPARAM wParam, LPARAM lParam)
{
	MsgHandler_ContextMenu* pThis;
	if (uiMsg == WM_NCCREATE)
	{
		pThis = static_cast<MsgHandler_ContextMenu*>(reinterpret_cast<CREATESTRUCT*>(lParam)->lpCreateParams);

		SetLastError(0);
		if (!SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pThis)))
		{
			if (GetLastError() != 0)
				return FALSE;
		}
	}
	else
	{
		pThis = reinterpret_cast<MsgHandler_ContextMenu*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
	}
	if (pThis)
	{
		if (pThis->g_pcm3) {
			LRESULT lres;
			if (SUCCEEDED(pThis->g_pcm3->HandleMenuMsg2(uiMsg, wParam, lParam, &lres))) {
				return lres;
			}
		}
		else if (pThis->g_pcm2) {
			if (SUCCEEDED(pThis->g_pcm2->HandleMenuMsg(uiMsg, wParam, lParam))) {
				return 0;
			}
		}
	}
	return DefWindowProc(hwnd, uiMsg, wParam, lParam);
}

void MsgHandler_ContextMenu::ShowContextMenuForFile(LPCWSTR filePath, UINT menuFlags, HINSTANCE hInstance)
{
	// Register the window class.
	const wchar_t CLASS_NAME[] = L"Files Window Class";

	WNDCLASS wc = { };
	wc.lpfnWndProc = WndProc;
	wc.hInstance = hInstance;
	wc.lpszClassName = CLASS_NAME;

	RegisterClass(&wc);

	HWND hwnd = CreateWindowEx(
		0,                            // Optional window styles.
		CLASS_NAME,                   // Window class
		L"Files Fulltrust",           // Window text
		0,                            // Window style (WS_OVERLAPPEDWINDOW)
		0, 0, 0, 0,    // Size and position
		HWND_MESSAGE,  // Parent window (NULL)
		NULL,          // Menu
		wc.hInstance,  // Instance handle
		this           // Additional application data
	);

	ShowWindow(hwnd, SW_SHOW);

	IContextMenu* pcm;
	if (SUCCEEDED(GetUIObjectOfFile(hwnd, filePath, IID_IContextMenu, (void**)&pcm)))
	{
		pcm->QueryInterface(IID_IContextMenu2, (void**)&g_pcm2);
		pcm->QueryInterface(IID_IContextMenu3, (void**)&g_pcm3);
		HMENU hmenu = CreatePopupMenu();
		if (hmenu) {
			if (SUCCEEDED(pcm->QueryContextMenu(hmenu, 0,
				SCRATCH_QCM_FIRST, SCRATCH_QCM_LAST,
				menuFlags))) {
				//auto itemCount = GetMenuItemCount(hmenu);
				int iCmd = TrackPopupMenuEx(hmenu, TPM_RETURNCMD, 0, 0, hwnd, NULL);
				if (iCmd > 0) {
					//char pszName[MAX_PATH];
					//pcm->GetCommandString(iCmd - SCRATCH_QCM_FIRST, GCS_VERBA, NULL, pszName, MAX_PATH);
					CMINVOKECOMMANDINFOEX info = { 0 };
					info.cbSize = sizeof(info);
					info.fMask = CMIC_MASK_UNICODE;
					info.hwnd = hwnd;
					info.lpVerb = MAKEINTRESOURCEA(iCmd - SCRATCH_QCM_FIRST);
					info.lpVerbW = MAKEINTRESOURCEW(iCmd - SCRATCH_QCM_FIRST);
					info.nShow = SW_SHOW;
					pcm->InvokeCommand((LPCMINVOKECOMMANDINFO)&info);
				}
			}
			DestroyMenu(hmenu);
		}
		if (g_pcm2) {
			g_pcm2->Release();
			g_pcm2 = NULL;
		}
		if (g_pcm3) {
			g_pcm3->Release();
			g_pcm3 = NULL;
		}
		pcm->Release();
	}
}

HRESULT MsgHandler_ContextMenu::GetUIObjectOfFile(HWND hwnd, LPCWSTR pszPath, REFIID riid, void** ppv)
{
	*ppv = NULL;
	HRESULT hr;
	LPITEMIDLIST pidl;
	SFGAOF sfgao;
	if (SUCCEEDED(hr = SHParseDisplayName(pszPath, NULL, &pidl, 0, &sfgao))) {
		IShellFolder* psf;
		LPCITEMIDLIST pidlChild;
		if (SUCCEEDED(hr = SHBindToParent(pidl, IID_IShellFolder,
			(void**)&psf, &pidlChild))) {

			//STRRET strDispName;
			//TCHAR pszParseName[MAX_PATH];
			//psf->GetDisplayNameOf(pidlChild, SHGDN_FORPARSING, &strDispName);
			//StrRetToBuf(&strDispName, pidlChild, pszParseName, MAX_PATH);

			//SHELLEXECUTEINFO ShExecInfo;
			//ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
			//ShExecInfo.fMask = NULL;
			//ShExecInfo.hwnd = NULL;
			//ShExecInfo.lpVerb = NULL;
			//ShExecInfo.lpFile = pszParseName;
			//ShExecInfo.lpParameters = NULL;
			//ShExecInfo.lpDirectory = NULL;
			//ShExecInfo.nShow = SW_MAXIMIZE;
			//ShExecInfo.hInstApp = NULL;
			//ShellExecuteEx(&ShExecInfo);

			hr = psf->GetUIObjectOf(hwnd, 1, &pidlChild, riid, NULL, ppv);
			psf->Release();
		}
		CoTaskMemFree(pidl);
	}
	return hr;
}

IAsyncOperation<bool> MsgHandler_ContextMenu::ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args)
{
	if (args.Request().Message().HasKey(L"Arguments"))
	{
		auto arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();
		if (arguments == L"LoadContextMenu")
		{
			auto filePath = args.Request().Message().Lookup(L"FilePath").as<hstring>();
			auto extendedMenu = args.Request().Message().Lookup(L"ExtendedMenu").as<bool>();
			auto showOpenMenu = args.Request().Message().Lookup(L"ShowOpenMenu").as<bool>();
			ShowContextMenuForFile(filePath.c_str(), CMF_NORMAL, GetModuleHandle(0));
			co_return TRUE;
		}
	}
	co_return FALSE;
}
