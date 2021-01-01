#include "pch.h"
#include <windows.h>

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::ApplicationModel;
using namespace Windows::ApplicationModel::AppService;
using namespace Windows::Storage;

HANDLE ghConnectionCloseEvent;

IAsyncAction InitializeAppServiceConnection();
AppServiceConnection connection;
IAsyncAction Connection_RequestReceived(AppServiceConnection const& sender, AppServiceRequestReceivedEventArgs const& args);
void Connection_ServiceClosed(AppServiceConnection const& sender, AppServiceClosedEventArgs const& args);
IAsyncAction ParseArgumentsAsync(AppServiceRequestReceivedEventArgs const& args, AppServiceDeferral messageDeferral, hstring arguments, ApplicationDataContainer localSettings);

// Properties -> Linker -> System -> Subsystem = /SUBSYSTEM:Windows
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
		init_apartment(apartment_type::single_threaded);

		printf("Files Fulltrust\n");

		ghConnectionCloseEvent = CreateEvent(
			NULL, TRUE, FALSE, TEXT("CnnectionCloseEvent")
		);
		if (ghConnectionCloseEvent == NULL)
		{
			printf("CreateEvent failed (%d)\n", GetLastError());
			return 1;
		}

		InitializeAppServiceConnection();

		WaitForSingleObject(ghConnectionCloseEvent, INFINITE);
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

IAsyncAction InitializeAppServiceConnection()
{
	connection.AppServiceName(L"FilesInteropService");
	connection.PackageFamilyName(Package::Current().Id().FamilyName());
	connection.RequestReceived(Connection_RequestReceived);
	connection.ServiceClosed(Connection_ServiceClosed);

	AppServiceConnectionStatus status = co_await connection.OpenAsync();
	if (status != AppServiceConnectionStatus::Success)
	{
		// TODO: error handling
		connection = NULL;
	}
}

IAsyncAction Connection_RequestReceived(AppServiceConnection const& sender, AppServiceRequestReceivedEventArgs const& args)
{
	auto messageDeferral = args.GetDeferral();
	if (args.Request().Message() == NULL)
	{
		messageDeferral.Complete();
		co_return;
	}

	try
	{
		if (args.Request().Message().HasKey(L"Arguments"))
		{
			auto arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();
			printf("Request received: %ls\n", arguments.c_str());
			DEBUGPRINTF(L"Request received: %ls\n", arguments.c_str());

			auto localSettings = ApplicationData::Current().LocalSettings();
			co_await ParseArgumentsAsync(args, messageDeferral, arguments, localSettings);
		}
	}
	catch (std::exception e)
	{
		printf(e.what());
	}

	messageDeferral.Complete();
}

IAsyncAction ParseArgumentsAsync(AppServiceRequestReceivedEventArgs const& args, AppServiceDeferral messageDeferral, hstring arguments, ApplicationDataContainer localSettings)
{
	if (arguments == L"Terminate")
	{
		// Exit fulltrust process (UWP is closed or suspended)
		SetEvent(ghConnectionCloseEvent);
		messageDeferral.Complete();
		co_return;
	}
}

void Connection_ServiceClosed(AppServiceConnection const& sender, AppServiceClosedEventArgs const& args)
{
	// Signal the event so the process can shut down
	SetEvent(ghConnectionCloseEvent);
}
