#include "pch.h"
#include "MsgHandler_ContextMenu.h"
#include <sstream>
//#include <windowsx.h>

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
	else if (uiMsg == WM_DESTROY)
	{
		PostQuitMessage(0);
		pThis = NULL;
	}
	else
	{
		pThis = reinterpret_cast<MsgHandler_ContextMenu*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
	}
	if (pThis)
	{
		if (uiMsg == WM_CONTEXTMENU)
		{
			pThis->ShowContextMenuForFile(reinterpret_cast<MenuArgs*>(lParam));
			SetEvent(reinterpret_cast<HANDLE>(wParam));
		}
		else if (pThis->g_pcm3)
		{
			LRESULT lres;
			if (SUCCEEDED(pThis->g_pcm3->HandleMenuMsg2(uiMsg, wParam, lParam, &lres)))
			{
				return lres;
			}
		}
		else if (pThis->g_pcm2)
		{
			if (SUCCEEDED(pThis->g_pcm2->HandleMenuMsg(uiMsg, wParam, lParam)))
			{
				return 0;
			}
		}
	}
	return DefWindowProc(hwnd, uiMsg, wParam, lParam);
}

void MsgHandler_ContextMenu::ShowContextMenuForFile(MenuArgs* args)
{
	HWND hwnd = this->hiddenWindow;
	IContextMenu* pcm;
	if (SUCCEEDED(GetUIObjectOfFile(hwnd, args->FileList, IID_IContextMenu, (void**)&pcm)))
	{
		pcm->QueryInterface(IID_IContextMenu2, (void**)&g_pcm2);
		pcm->QueryInterface(IID_IContextMenu3, (void**)&g_pcm3);
		HMENU hmenu = CreatePopupMenu();
		if (hmenu)
		{
			if (SUCCEEDED(pcm->QueryContextMenu(hmenu, 0,
				SCRATCH_QCM_FIRST, SCRATCH_QCM_LAST,
				args->ExtendedMenu ? CMF_EXTENDEDVERBS : CMF_NORMAL)))
			{
				POINT pt;
				GetCursorPos(&pt);
				//auto itemCount = GetMenuItemCount(hmenu);
				int iCmd = TrackPopupMenuEx(hmenu, TPM_RETURNCMD, pt.x, pt.y, hwnd, NULL);
				this->clickedItem = iCmd;
				if (iCmd > 0)
				{
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
		if (g_pcm2)
		{
			g_pcm2->Release();
			g_pcm2 = NULL;
		}
		if (g_pcm3)
		{
			g_pcm3->Release();
			g_pcm3 = NULL;
		}
		pcm->Release();
	}
}

HRESULT MsgHandler_ContextMenu::GetUIObjectOfFile(HWND hwnd, std::vector<std::wstring> fileList, REFIID riid, void** ppv)
{
	*ppv = NULL;
	HRESULT hr;
	PIDLIST_ABSOLUTE pidl;
	SFGAOF sfgao;
	if (fileList.empty())
	{
		return -1;
	}

	auto firstElem = fileList[0].c_str();
	if (SUCCEEDED(hr = SHParseDisplayName(firstElem, NULL, &pidl, 0, &sfgao)))
	{
		IShellFolder* psf;
		if (SUCCEEDED(hr = SHBindToParent(pidl, IID_IShellFolder,
			(void**)&psf, NULL)))
		{
			PUITEMID_CHILD* rgpidl = (PUITEMID_CHILD*)CoTaskMemAlloc(sizeof(PUITEMID_CHILD) * fileList.size());
			if (rgpidl != NULL)
			{
				int parsedChildren = 0;
				for (; parsedChildren < fileList.size(); parsedChildren++)
				{
					PIDLIST_ABSOLUTE pidlChild;
					if (SUCCEEDED(hr = SHParseDisplayName(fileList[parsedChildren].c_str(), NULL, &pidlChild, 0, &sfgao)))
					{
						// if (ILFindChild(pidl, pidlChild) == NULL) // Not a child of parent folder
						rgpidl[parsedChildren] = (PUITEMID_CHILD)(ILClone(ILFindLastID(pidlChild)));
						CoTaskMemFree(pidlChild);
					}
					else
					{
						break;
					}
				}
				if (SUCCEEDED(hr))
				{
					hr = psf->GetUIObjectOf(hwnd, fileList.size(), rgpidl, riid, NULL, ppv);
				}
				for (int freeIdx = 0; freeIdx < parsedChildren; freeIdx++)
				{
					CoTaskMemFree(rgpidl[freeIdx]);
				}
				CoTaskMemFree(rgpidl);
			}
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

			MenuArgs* args = new MenuArgs();
			args->ExtendedMenu = extendedMenu;
			args->ShowOpenMenu = showOpenMenu;
			std::wstringstream stringStream(filePath.c_str());
			std::wstring item;
			while (std::getline(stringStream, item, L'|'))
			{
				args->FileList.push_back(item);
			}

			if (this->hiddenWindow != NULL)
			{
				HANDLE ghMenuCloseEvent = CreateEvent(
					NULL, TRUE, FALSE, TEXT("MenuCloseEvent"));
				PostMessage(this->hiddenWindow, WM_CONTEXTMENU, reinterpret_cast<LONG_PTR>(ghMenuCloseEvent), reinterpret_cast<LONG_PTR>(args));
				WaitForSingleObject(ghMenuCloseEvent, INFINITE);
				CloseHandle(ghMenuCloseEvent);
				printf("Clicked item: %d\n", clickedItem);
			}

			delete args;

			co_return TRUE;
		}
	}
	co_return FALSE;
}

void MsgHandler_ContextMenu::CreateHiddenWindow(HINSTANCE hInstance)
{
	(void)CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);

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

	this->hiddenWindow = hwnd;

	MSG msg;
	while (GetMessage(&msg, NULL, 0, 0))
	{
		//TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
}

MsgHandler_ContextMenu::MsgHandler_ContextMenu(HINSTANCE hInstance)
{
	this->windowThread = std::thread(&MsgHandler_ContextMenu::CreateHiddenWindow, this, hInstance);
}

MsgHandler_ContextMenu::~MsgHandler_ContextMenu()
{
	if (this->hiddenWindow != NULL)
	{
		PostMessage(this->hiddenWindow, WM_CLOSE, NULL, NULL);
	}
	if (this->windowThread.joinable())
	{
		this->windowThread.join();
	}
}
