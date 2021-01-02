#include "pch.h"
#include "AppServiceManager.h"

AppServiceManager* AppServiceManager::Init()
{
	AppServiceManager* manager = new AppServiceManager();
	manager->ghConnectionCloseEvent = CreateEvent(
		NULL, TRUE, FALSE, TEXT("ConnectionCloseEvent")
	);
	if (manager->ghConnectionCloseEvent == NULL)
	{
		printf("CreateEvent failed (%d)\n", GetLastError());
		return NULL;
	}
	manager->InitializeAppServiceConnection();
	return manager;
}

void AppServiceManager::Loop()
{
	WaitForSingleObject(ghConnectionCloseEvent, INFINITE);
	CloseHandle(ghConnectionCloseEvent);
}

IAsyncAction AppServiceManager::InitializeAppServiceConnection()
{
	AppServiceConnection conn;
	conn.AppServiceName(L"FilesInteropService");
	conn.PackageFamilyName(Package::Current().Id().FamilyName());
	conn.RequestReceived({ this, &AppServiceManager::Connection_RequestReceived });
	conn.ServiceClosed({ this, &AppServiceManager::Connection_ServiceClosed });

	AppServiceConnectionStatus status = co_await conn.OpenAsync();
	if (status != AppServiceConnectionStatus::Success)
	{
		// TODO: error handling
		SetEvent(ghConnectionCloseEvent);
		connection = NULL;
	}
	else
	{
		connection = conn;
	}
}

IAsyncAction AppServiceManager::Connection_RequestReceived(AppServiceConnection const& sender, AppServiceRequestReceivedEventArgs const& args)
{
	auto messageDeferral = args.GetDeferral();
	if (args.Request().Message() == NULL)
	{
		messageDeferral.Complete();
		co_return;
	}

	try
	{
		co_await ParseArgumentsAsync(args);
	}
	catch (std::exception e)
	{
		printf(e.what());
	}

	messageDeferral.Complete();
}

IAsyncAction AppServiceManager::ParseArgumentsAsync(AppServiceRequestReceivedEventArgs const& args)
{
	if (args.Request().Message().HasKey(L"Arguments"))
	{
		auto arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();
		if (arguments == L"Terminate")
		{
			// Exit fulltrust process (UWP is closed or suspended)
			SetEvent(ghConnectionCloseEvent);
			co_return;
		}
	}
	for (MessageHandler* handler : messageHandlers)
	{
		if (co_await handler->ParseArgumentsAsync(*this, args))
		{
			break;
		}
	}
}

void AppServiceManager::Connection_ServiceClosed(AppServiceConnection const& sender, AppServiceClosedEventArgs const& args)
{
	// Signal the event so the process can shut down
	SetEvent(ghConnectionCloseEvent);
}

IAsyncOperation<AppServiceResponse> AppServiceManager::Send(ValueSet message)
{
	if (connection != NULL)
	{
		return connection.SendMessageAsync(message);
	}
	return NULL;
}
