#pragma once

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
