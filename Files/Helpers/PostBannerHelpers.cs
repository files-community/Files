using Files.Enums;
using Microsoft.Toolkit.Uwp;
using System.Diagnostics;

namespace Files.Helpers
{
    public static class PostBannerHelpers
    {
        public static void PostBanner_Delete(ReturnResult status, FileOperationType operation, Stopwatch sw, IShellPage associatedInstance)
        {
            if (status == ReturnResult.Failed ||
                status == ReturnResult.UnknownException ||
                status == ReturnResult.IntegrityCheckFailed ||
                status == ReturnResult.AccessUnauthorized)
            {
                if (status == ReturnResult.AccessUnauthorized)
                {
                    App.StatusCenterViewModel.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.IntegrityCheckFailed)
                {
                    App.StatusCenterViewModel.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.Failed || status == ReturnResult.UnknownException)
                {
                    App.StatusCenterViewModel.PostBanner(
                        "StatusDeletionFailed".GetLocalized(),
                        "StatusUnknownError".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (sw.Elapsed.TotalSeconds >= 10)
                {
                    if (operation == FileOperationType.Delete)
                    {
                        App.StatusCenterViewModel.PostBanner(
                        "StatusDeletionComplete".GetLocalized(),
                        "StatusOperationCompleted".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                    else if (operation == FileOperationType.Recycle)
                    {
                        App.StatusCenterViewModel.PostBanner(
                        "StatusRecycleComplete".GetLocalized(),
                        "StatusOperationCompleted".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                }
            }
        }
    }
}