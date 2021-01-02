#include "pch.h"
#include "AppServiceManager.h"
#include "MsgHandler_ContextMenu.h"

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

	try
	{
		init_apartment();

		printf("Files Fulltrust\n");

		auto manager = AppServiceManager::Init();
		if (manager != NULL)
		{
			MsgHandler_ContextMenu cmHdl;
			manager->Register(&cmHdl);

			manager->Loop();
			delete manager;
		}
	}
	catch (std::exception e)
	{
		CloseHandle(ghMutex);
		printf(e.what());
		return 1;
	}

	CloseHandle(ghMutex);

	return 0;
}
