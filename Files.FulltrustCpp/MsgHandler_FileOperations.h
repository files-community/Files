#pragma once
#include "MessageHandler.h"
#include "AppServiceManager.h"

struct IconResponse
{
	std::string Icon;
	std::string Overlay;
	bool IsCustom;
};

struct LinkResponse
{
	std::wstring TargetPath;
	std::wstring Arguments;
	std::wstring WorkingDirectory;
	bool RunAsAdmin;
	bool IsFolder;
};

class MsgHandler_FileOperations : public MessageHandler
{
	IconResponse GetFileIconAndOverlay(LPCWSTR fileIconPath, int thumbnailSize);

	bool ParseLink(LPCWSTR linkFilePath, LinkResponse& resp);
	void SaveLink(LPCWSTR linkFilePath, LinkResponse const& link);

public:
	IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);
};
