#include "pch.h"
#include "MsgHandler_RecycleBin.h"
#include "NativeMethods.h"
#include <propkey.h>
#include <filesystem>

std::wstring_convert<std::codecvt_utf8<wchar_t>> MsgHandler_RecycleBin::myconv;

std::wstring MsgHandler_RecycleBin::GetRecycleBinDisplayName()
{
	std::wstring result;
	PIDLIST_ABSOLUTE pidlRecycleBin;
	if (SUCCEEDED(SHGetKnownFolderIDList(FOLDERID_RecycleBinFolder, 0, NULL, &pidlRecycleBin)))
	{
		IShellFolder* psf;
		PCUITEMID_CHILD relativePidl;
		if (SUCCEEDED(SHBindToParent(pidlRecycleBin, IID_IShellFolder, (void**)&psf, &relativePidl)))
		{
			result = GetDisplayName(psf, (PITEMID_CHILD)relativePidl, SIGDN_NORMALDISPLAY);
			psf->Release();
		}
		CoTaskMemFree(pidlRecycleBin);
	}
	return result;
}

ShellFileItem MsgHandler_RecycleBin::GetShellItem(IShellItem2* iItem)
{
	ShellFileItem shellItem;

	shellItem.RecyclePath = myconv.to_bytes(GetDisplayName(iItem, SIGDN_FILESYSPATH));
	auto displayName = GetDisplayName(iItem, SIGDN_NORMALDISPLAY);

	shellItem.FileName = myconv.to_bytes(std::filesystem::path(displayName).filename());
	shellItem.FilePath = myconv.to_bytes(displayName);
	SFGAOF sfgao;
	if (SUCCEEDED(iItem->GetAttributes(SFGAO_FOLDER, &sfgao)))
	{
		shellItem.IsFolder = (sfgao & SFGAO_FOLDER);
	}
	FILETIME createFt = { 0 };
	if (SUCCEEDED(iItem->GetFileTime(PKEY_DateCreated, &createFt)))
	{
		shellItem.RecycleDate = ((ULONGLONG)createFt.dwHighDateTime << 32) + (UINT)createFt.dwLowDateTime;
	}
	ULONG fileSizeBytes = 0;
	if (SUCCEEDED(iItem->GetUInt32(PKEY_Size, &fileSizeBytes)))
	{
		shellItem.FileSizeBytes = fileSizeBytes;
	}
	LPWSTR fileType = NULL;
	if (SUCCEEDED(iItem->GetString(PKEY_ItemTypeText, &fileType)))
	{
		shellItem.FileType = myconv.to_bytes(fileType);
		CoTaskMemFree(fileType);
	}
	IPropertyDescription* propDesc;
	if (SUCCEEDED(PSGetPropertyDescription(PKEY_Size, IID_IPropertyDescription, (LPVOID*)&propDesc)))
	{
		PROPVARIANT propVar;
		if (SUCCEEDED(iItem->GetProperty(PKEY_Size, &propVar)))
		{
			LPWSTR propAsStr;
			if (SUCCEEDED(propDesc->FormatForDisplay(propVar, PDFF_DEFAULT, &propAsStr)))
			{
				shellItem.FileSize = myconv.to_bytes(propAsStr);
				CoTaskMemFree(propAsStr);
			}
		}
		propDesc->Release();
	}

	return shellItem;
}

std::list<ShellFileItem> MsgHandler_RecycleBin::EnumerateRecycleBin()
{
	std::list<ShellFileItem> shellItems;
	PIDLIST_ABSOLUTE pidlRecycleBin;
	if (SUCCEEDED(SHGetKnownFolderIDList(FOLDERID_RecycleBinFolder, 0, NULL, &pidlRecycleBin)))
	{
		IShellFolder* psf;
		PCUITEMID_CHILD relativePidl;
		if (SUCCEEDED(SHBindToParent(pidlRecycleBin, IID_IShellFolder, (void**)&psf, &relativePidl)))
		{
			IShellFolder* recycleBinFolder;
			if (SUCCEEDED(psf->BindToObject(relativePidl, NULL, IID_IShellFolder, (LPVOID*)&recycleBinFolder)))
			{
				LPENUMIDLIST penumFiles;
				if (SUCCEEDED(recycleBinFolder->EnumObjects(NULL, 
					SHCONTF_FOLDERS | SHCONTF_NONFOLDERS | SHCONTF_INCLUDEHIDDEN, 
					&penumFiles)))
				{
					PITEMID_CHILD pidl;
					while (penumFiles->Next(1, &pidl, NULL) != S_FALSE)
					{
						IShellItem2* iItem;
						if (SUCCEEDED(SHCreateItemWithParent(NULL, recycleBinFolder, pidl, IID_IShellItem2, (LPVOID*)&iItem)))
						{
							auto shellItem = GetShellItem(iItem);
							shellItems.push_back(shellItem);
							iItem->Release();
						}
					}
					penumFiles->Release();
				}
				recycleBinFolder->Release();
			}
			psf->Release();
		}
		CoTaskMemFree(pidlRecycleBin);
	}
	return shellItems;
}

IAsyncOperation<bool> MsgHandler_RecycleBin::ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args)
{
	if (args.Request().Message().HasKey(L"Arguments"))
	{
		auto arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();
		if (arguments == L"RecycleBin")
		{
			auto binAction = args.Request().Message().Lookup(L"action").as<hstring>();
			if (binAction == L"Empty")
			{
				// Shell function to empty recyclebin
				SHEmptyRecycleBin(NULL, NULL, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI);
			}
			else if (binAction == L"Query")
			{
				ValueSet responseQuery;
				SHQUERYRBINFO queryBinInfo;
				queryBinInfo.cbSize = sizeof(queryBinInfo);
				auto res = SHQueryRecycleBin(L"", &queryBinInfo);
				if (SUCCEEDED(res))
				{
					auto numItems = queryBinInfo.i64NumItems;
					auto binSize = queryBinInfo.i64Size;
					responseQuery.Insert(L"NumItems", winrt::box_value(numItems));
					responseQuery.Insert(L"BinSize", winrt::box_value(binSize));
					co_await args.Request().SendResponseAsync(responseQuery);
				}
			}
			else if (binAction == L"Enumerate")
			{
				// Enumerate recyclebin contents and send response to UWP
				auto serializedContent = json(this->EnumerateRecycleBin()).dump();
				ValueSet responseEnum;
				responseEnum.Insert(L"Enumerate", winrt::box_value(winrt::to_hstring(serializedContent)));
				co_await args.Request().SendResponseAsync(responseEnum);
			}

			co_return TRUE;
		}
	}

	co_return FALSE;
}
