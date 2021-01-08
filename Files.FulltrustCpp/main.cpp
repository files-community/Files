#include "pch.h"
#include "AppServiceManager.h"
#include "MsgHandler_ContextMenu.h"
#include "MsgHandler_FileOperations.h"
#include "MsgHandler_RecycleBin.h"

using namespace winrt;

// To hide the console window
// Project Properties -> Linker -> System -> Subsystem = /SUBSYSTEM:Windows
//int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
int main()
{
	auto ghMutex = CreateMutex(NULL, TRUE, L"Local\\FilesUwpFullTrust");
	if (ghMutex == NULL)
	{
		printf("CreateMutex failed (%d)\n", GetLastError());
		return 1;
	}
	if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		printf("Process already running\n");
		return 0;
	}

	// Init WinRT
	init_apartment();

	// Init GDIPlus
	Gdiplus::GdiplusStartupInput gdiplusStartupInput;
	ULONG_PTR gdiplusToken;
	Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

	printf("Files Fulltrust\n");

	auto manager = AppServiceManager::Init();
	if (manager != NULL)
	{
		MsgHandler_ContextMenu cmHdl(GetModuleHandle(0)); //hInstance
		manager->Register(&cmHdl);
		MsgHandler_FileOperations foHdl;
		manager->Register(&foHdl);
		MsgHandler_RecycleBin rbHdl;
		manager->Register(&rbHdl);

		manager->Loop();
		delete manager;
	}

	// Unload GDIPlus
	Gdiplus::GdiplusShutdown(gdiplusToken);

	CloseHandle(ghMutex);

	return 0;
}
