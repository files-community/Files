#pragma once
#include "MessageHandler.h"
#include "AppServiceManager.h"
#include <codecvt>

using json = nlohmann::json;

struct ShellFileItem
{
    bool IsFolder;
    std::string RecyclePath;
    std::string FileName;
    std::string FilePath;
    LONGLONG RecycleDate;
    std::string FileSize;
    ULONG FileSizeBytes;
    std::string FileType;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(ShellFileItem, IsFolder, RecyclePath, FileName, FilePath, RecycleDate, FileSize, FileSizeBytes, FileType);
};

class MsgHandler_RecycleBin : public MessageHandler
{
    ShellFileItem GetShellItem(IShellItem2* iItem);

    static std::wstring_convert<std::codecvt_utf8<wchar_t>> myconv;

public:
    std::list<ShellFileItem> EnumerateRecycleBin();

	IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);

    static std::wstring GetRecycleBinDisplayName();
};
