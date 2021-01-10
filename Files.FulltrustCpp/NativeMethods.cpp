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
	std::string result;
	BITMAP nativeBitmap;
	Gdiplus::Bitmap* gdiBitmap = NULL;
	if (GetObject(hIcon, sizeof(nativeBitmap), (LPVOID)&nativeBitmap) && nativeBitmap.bmBits)
	{
		gdiBitmap = new Gdiplus::Bitmap(nativeBitmap.bmWidth, nativeBitmap.bmHeight, PixelFormat32bppARGB);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
		Gdiplus::BitmapData data;
		Gdiplus::Rect rect(0, 0, nativeBitmap.bmWidth, nativeBitmap.bmHeight);
		gdiBitmap->LockBits(&rect,
			Gdiplus::ImageLockModeWrite, gdiBitmap->GetPixelFormat(), &data);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
		if (data.Stride != nativeBitmap.bmWidthBytes)
		{
			delete gdiBitmap; // pixel_format is wrong
			return result;
		}
		//memcpy(data.Scan0, nativeBitmap.bmBits, nativeBitmap.bmHeight * nativeBitmap.bmWidthBytes);
		for (int ll = 0; ll < nativeBitmap.bmHeight; ll++)
		{
			// Flip image upside-down, check winrar icon
			memcpy((char*)data.Scan0 + nativeBitmap.bmWidthBytes * ll,
				(char*)nativeBitmap.bmBits + nativeBitmap.bmWidthBytes * (nativeBitmap.bmHeight - 1 - ll),
				nativeBitmap.bmWidthBytes);
		}
		gdiBitmap->UnlockBits(&data);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
	}
	else
	{
		return result;
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

std::string IconToBase64String(HBITMAP hBitmap)
{
	std::string result;
	BITMAP nativeBitmap;
	Gdiplus::Bitmap* gdiBitmap = NULL;
	if (GetObject(hBitmap, sizeof(nativeBitmap), (LPVOID)&nativeBitmap) && nativeBitmap.bmBits)
	{
		gdiBitmap = new Gdiplus::Bitmap(nativeBitmap.bmWidth, nativeBitmap.bmHeight, PixelFormat32bppARGB);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
		Gdiplus::BitmapData data;
		Gdiplus::Rect rect(0, 0, nativeBitmap.bmWidth, nativeBitmap.bmHeight);
		gdiBitmap->LockBits(&rect,
			Gdiplus::ImageLockModeWrite, gdiBitmap->GetPixelFormat(), &data);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
		if (data.Stride != nativeBitmap.bmWidthBytes)
		{
			delete gdiBitmap; // pixel_format is wrong
			return result;
		}
		//memcpy(data.Scan0, nativeBitmap.bmBits, nativeBitmap.bmHeight * nativeBitmap.bmWidthBytes);
		for (int ll = 0; ll < nativeBitmap.bmHeight; ll++)
		{
			// Flip image upside-down, check winrar icon
			memcpy((char*)data.Scan0 + nativeBitmap.bmWidthBytes * ll,
				(char*)nativeBitmap.bmBits + nativeBitmap.bmWidthBytes * (nativeBitmap.bmHeight - 1 - ll),
				nativeBitmap.bmWidthBytes);
		}
		gdiBitmap->UnlockBits(&data);
		if (gdiBitmap->GetLastStatus() != Gdiplus::Status::Ok)
		{
			delete gdiBitmap;
			return result;
		}
	}
	else
	{
		gdiBitmap = new Gdiplus::Bitmap(hBitmap, nullptr);
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
