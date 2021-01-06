#include "pch.h"
#include "MsgHandler_FileOperations.h"
#include "NativeMethods.h"

IconResponse MsgHandler_FileOperations::GetFileIconAndOverlay(LPCWSTR fileIconPath, int thumbnailSize)
{
	IconResponse resp{ "", "", false };

	SHFILEINFO shfi;
	ZeroMemory(&shfi, sizeof(shfi));
	if (!SHGetFileInfo(fileIconPath, 0, &shfi, sizeof(SHFILEINFO), SHGFI_OVERLAYINDEX | SHGFI_ICON | SHGFI_SYSICONINDEX | SHGFI_ICONLOCATION))
	{
		return resp;
	}

	wchar_t winDir[MAX_PATH];
	SHGetSpecialFolderPath(NULL, winDir, CSIDL_WINDOWS, FALSE);
	std::wstring iconPath = shfi.szDisplayName;

	resp.IsCustom = iconPath.rfind(winDir, 0) != 0;
	DestroyIcon(shfi.hIcon);
	IImageList* imageList;
	if (!SUCCEEDED(SHGetImageList(SHIL_LARGE, IID_IImageList, (void**)&imageList)))
	{
		return resp;
	}

	auto overlay_idx = shfi.iIcon >> 24;
	if (overlay_idx != 0)
	{
		int overlay_image;
		if (SUCCEEDED(imageList->GetOverlayImage(overlay_idx, &overlay_image)))
		{
			HICON hOverlay;
			if (SUCCEEDED(imageList->GetIcon(overlay_image, ILD_TRANSPARENT, &hOverlay)))
			{
				resp.Overlay = IconToBase64String(hOverlay);
				DestroyIcon(hOverlay);
			}
		}
	}

	imageList->Release();

	IShellItem* psi;
	if (SUCCEEDED(SHCreateItemFromParsingName(fileIconPath, NULL, IID_IShellItem, (void**)&psi)))
	{
		IShellItemImageFactory* psiif = NULL;
		if (SUCCEEDED(psi->QueryInterface(IID_IShellItemImageFactory, (void**)&psiif)))
		{
			int flags = SIIGBF_BIGGERSIZEOK;
			if (thumbnailSize < 80) flags = flags | SIIGBF_ICONONLY;

			HBITMAP hBitmap = NULL;
			SIZE iconSize = { thumbnailSize, thumbnailSize };
			if (SUCCEEDED(psiif->GetImage(iconSize, flags, &hBitmap)))
			{
				resp.Icon = IconToBase64String(hBitmap);
				DeleteObject(hBitmap);
			}

			psiif->Release();
		}
		psi->Release();
	}

	return resp;
}

IAsyncOperation<bool> MsgHandler_FileOperations::ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args)
{
	if (args.Request().Message().HasKey(L"Arguments"))
	{
		auto arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();
		if (arguments == L"GetIconOverlay")
		{
			auto fileIconPath = args.Request().Message().Lookup(L"filePath").as<hstring>();
			auto thumbnailSize = args.Request().Message().Lookup(L"thumbnailSize").as<int>();
			auto iconOverlay = GetFileIconAndOverlay(fileIconPath.c_str(), thumbnailSize);

			ValueSet valueSet;
			valueSet.Insert(L"Icon", winrt::box_value(winrt::to_hstring(iconOverlay.Icon)));
			valueSet.Insert(L"Overlay", winrt::box_value(winrt::to_hstring(iconOverlay.Overlay)));
			valueSet.Insert(L"HasCustomIcon", winrt::box_value(iconOverlay.IsCustom));
			co_await args.Request().SendResponseAsync(valueSet);

			co_return TRUE;
		}
	}

	co_return FALSE;
}
