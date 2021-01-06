#pragma once
#include "MessageHandler.h"
#include "AppServiceManager.h"

struct IconResponse
{
    std::string Icon;
    std::string Overlay;
    bool IsCustom;
};

class MsgHandler_FileOperations : public MessageHandler
{
    IconResponse GetFileIconAndOverlay(LPCWSTR fileIconPath, int thumbnailSize);

public:
    IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);
};
