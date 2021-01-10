#include "pch.h"
#include "NativeMethods.h"
#include "base64.h"

std::wstring GetDisplayName(IShellItem* iItem, SIGDN flags)
{
	std::wstring result;
	LPWSTR pszDisplayName;
	if (SUCCEEDED(iItem->GetDisplayName(flags, &pszDisplayName)))
	{
		result = pszDisplayName;
		CoTaskMemFree(pszDisplayName);
	}
	return result;
}

std::string ExtractStringFromDLL(LPCWSTR dllName, int resourceIndex)
{
	HMODULE lib = LoadLibrary(dllName);
	if (lib != NULL)
	{
		CHAR value[512];
		LoadStringA(lib, resourceIndex, value, 512);
		//FreeLibrary(lib);
		return value;
	}
	return "";
}

std::string IconToBase64String(HICON hIcon)
{
	ICONINFO iconinfo;
	GetIconInfo(hIcon, &iconinfo);
	auto hBitmap = (HBITMAP)CopyImage(iconinfo.hbmColor, IMAGE_BITMAP, 0, 0, LR_CREATEDIBSECTION);
	auto result = IconToBase64String(hBitmap);
	DeleteObject(hBitmap);
	return result;
}

std::string IconToBase64String(HBITMAP hBitmap, bool isBottomUp)
{
	std::string result;
	DIBSECTION dibSection;
	Gdiplus::Bitmap* gdiBitmap = NULL;
	if (!GetObject(hBitmap, sizeof(dibSection), (LPVOID)&dibSection) || dibSection.dsBmih.biBitCount != 32)
	{
		gdiBitmap = new Gdiplus::Bitmap(hBitmap, nullptr);
	}
	else
	{
		BITMAP nativeBitmap = dibSection.dsBm;
		if (!nativeBitmap.bmBits)
		{
			return result;
		}
		if (isBottomUp)
		{
			gdiBitmap = new Gdiplus::Bitmap(nativeBitmap.bmWidth, nativeBitmap.bmHeight, -nativeBitmap.bmWidthBytes, PixelFormat32bppARGB, (BYTE*)nativeBitmap.bmBits + nativeBitmap.bmWidthBytes * (nativeBitmap.bmHeight - 1));
		}
		else
		{
			gdiBitmap = new Gdiplus::Bitmap(nativeBitmap.bmWidth, nativeBitmap.bmHeight, nativeBitmap.bmWidthBytes, PixelFormat32bppARGB, (BYTE*)nativeBitmap.bmBits);
		}
	}

	if (gdiBitmap != NULL)
	{
		std::vector<BYTE> data;

		//write to IStream
		IStream* istream = nullptr;
		if (SUCCEEDED(CreateStreamOnHGlobal(NULL, TRUE, &istream)))
		{
			CLSID clsid_png;
			if (SUCCEEDED(CLSIDFromString(L"{557cf406-1a04-11d3-9a73-0000f81ef32e}", &clsid_png))) // bmp: {557cf400-1a04-11d3-9a73-0000f81ef32e}
			{
				Gdiplus::Status status = gdiBitmap->Save(istream, &clsid_png);
				if (status == Gdiplus::Status::Ok)
				{
					//get memory handle associated with istream
					HGLOBAL hg = NULL;
					if (SUCCEEDED(GetHGlobalFromStream(istream, &hg)))
					{
						//copy IStream to buffer
						SIZE_T bufsize = GlobalSize(hg);
						data.resize(bufsize);

						//lock & unlock memory
						LPVOID pimage = GlobalLock(hg);
						memcpy(&data[0], pimage, bufsize);
						GlobalUnlock(hg);

						result = base64_encode(data.data(), data.size(), false);
					}
				}
			}
			istream->Release();
		}
		delete gdiBitmap;
	}
	return result;
}
