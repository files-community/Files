#pragma once
#include <list>
#include "MessageHandler.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::ApplicationModel;
using namespace Windows::ApplicationModel::AppService;
using namespace Windows::Storage;

class AppServiceManager
{
private:
	HANDLE ghConnectionCloseEvent;

	IAsyncAction InitializeAppServiceConnection();
	AppServiceConnection connection;
	IAsyncAction Connection_RequestReceived(AppServiceConnection const& sender, AppServiceRequestReceivedEventArgs const& args);
	void Connection_ServiceClosed(AppServiceConnection const& sender, AppServiceClosedEventArgs const& args);
	IAsyncAction ParseArgumentsAsync(AppServiceRequestReceivedEventArgs const& args);
	std::list<MessageHandler*> messageHandlers;

public:
	static AppServiceManager* Init();
	void Loop() const;
	IAsyncOperation<AppServiceResponse> Send(ValueSet message) const;
	void Register(MessageHandler* hdl)
	{
		messageHandlers.remove(hdl);
		messageHandlers.push_back(hdl);
	}
	void Unregister(MessageHandler* hdl)
	{
		messageHandlers.remove(hdl);
	}
};
