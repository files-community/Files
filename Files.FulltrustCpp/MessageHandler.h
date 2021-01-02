#pragma once
#include "pch.h"

class AppServiceManager;

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::ApplicationModel;
using namespace Windows::ApplicationModel::AppService;

class MessageHandler
{
public:
	virtual IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args) = 0;
};
