#include "pch.h"
#include "MsgHandler_FileOperations.h"
#include "NativeMethods.h"
#include <filesystem>

bool MsgHandler_FileOperations::ParseLink(LPCWSTR linkFilePath, LinkResponse& resp)
{
	int hr = -1;
	if (std::filesystem::path(linkFilePath).extension() == L".lnk")
	{
		IShellLink* psl = NULL;
		if (SUCCEEDED(hr = CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_IShellLink, (LPVOID *)&psl)))
		{
			IPersistFile* ppf = NULL;
			if (SUCCEEDED(hr = psl->QueryInterface(IID_IPersistFile, (LPVOID*)&ppf)))
			{
				if (SUCCEEDED(hr = ppf->Load(linkFilePath, STGM_READ)))
				{
					auto resolveFlags = (SLR_FLAGS)MAKELONG((USHORT)SLR_NO_UI_WITH_MSG_PUMP, 100); // Timeout
					if (SUCCEEDED(hr = psl->Resolve(NULL, resolveFlags)))
					{
						wchar_t targetPath[MAX_PATH];
						WIN32_FIND_DATA findData{};
						if (SUCCEEDED(hr = psl->GetPath(targetPath, MAX_PATH, &findData, SLGP_RAWPATH)))
						{
							resp.TargetPath = targetPath;
							resp.IsFolder = findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY;
						}
						wchar_t arguments[2048];
						if (hr = SUCCEEDED(psl->GetArguments(arguments, 2048)))
						{
							resp.Arguments = arguments;
						}
						wchar_t workingDir[MAX_PATH];
						if (hr = SUCCEEDED(psl->GetWorkingDirectory(workingDir, MAX_PATH)))
						{
							resp.WorkingDirectory = workingDir;
						}
						IShellLinkDataList* psldl = NULL;
						if (hr = SUCCEEDED(psl->QueryInterface(IID_IShellLinkDataList, (LPVOID*)&psldl)))
						{
							DWORD dwFlags;
							if (SUCCEEDED(psldl->GetFlags(&dwFlags)))
							{
								resp.RunAsAdmin = dwFlags & SLDF_RUNAS_USER;
							}
							psldl->Release();
						}

					}
				}
				ppf->Release();
			}
			psl->Release();
		}
	}
	else if (std::filesystem::path(linkFilePath).extension() == L".url")
	{

	}
	return SUCCEEDED(hr);
}

void MsgHandler_FileOperations::SaveLink(LPCWSTR linkFilePath, LinkResponse const& link)
{
	if (std::filesystem::path(linkFilePath).extension() == L".lnk")
	{

	}
	else if (std::filesystem::path(linkFilePath).extension() == L".url")
	{

	}
}

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
			HBITMAP hBitmap = NULL;
			SIZE iconSize = { thumbnailSize, thumbnailSize };
			int flags = SIIGBF_BIGGERSIZEOK;
			if (thumbnailSize < 80)
			{
				if (SUCCEEDED(psiif->GetImage(iconSize, flags | SIIGBF_ICONONLY, &hBitmap)))
				{
					resp.Icon = IconToBase64String(hBitmap);
					DeleteObject(hBitmap);
				}
			}
			else
			{
				if (SUCCEEDED(psiif->GetImage(iconSize, flags | SIIGBF_THUMBNAILONLY, &hBitmap)))
				{
					resp.Icon = IconToBase64String(hBitmap, false);
					DeleteObject(hBitmap);
				}
				else if(SUCCEEDED(psiif->GetImage(iconSize, SIIGBF_BIGGERSIZEOK, &hBitmap)))
				{
					resp.Icon = IconToBase64String(hBitmap);
					DeleteObject(hBitmap);
				}
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
		else if (arguments == L"FileOperation")
		{
			auto fileOp = args.Request().Message().Lookup(L"fileop").as<hstring>();

			if (fileOp == L"ParseLink")
			{
				auto linkPath = args.Request().Message().Lookup(L"filepath").as<hstring>();
				LinkResponse link;
				if (ParseLink(linkPath.c_str(), link))
				{
					ValueSet valueSet;
					valueSet.Insert(L"TargetPath", winrt::box_value(link.TargetPath));
					valueSet.Insert(L"Arguments", winrt::box_value(link.Arguments));
					valueSet.Insert(L"WorkingDirectory", winrt::box_value(link.WorkingDirectory));
					valueSet.Insert(L"RunAsAdmin", winrt::box_value(link.RunAsAdmin));
					valueSet.Insert(L"IsFolder", winrt::box_value(link.IsFolder));
					co_await args.Request().SendResponseAsync(valueSet);
				}
			}
			else if (fileOp == L"CreateLink" || fileOp == L"UpdateLink")
			{
				auto linkSavePath = args.Request().Message().Lookup(L"filepath").as<hstring>();
				LinkResponse link;
				link.TargetPath = args.Request().Message().Lookup(L"targetpath").as<hstring>();
				link.Arguments = args.Request().Message().Lookup(L"arguments").as<hstring>();
				link.WorkingDirectory = args.Request().Message().Lookup(L"workingdir").as<hstring>();
				link.RunAsAdmin = args.Request().Message().Lookup(L"runasadmin").as<bool>();
				SaveLink(linkSavePath.c_str(), link);
			}

			co_return TRUE;
		}
	}

	co_return FALSE;
}
