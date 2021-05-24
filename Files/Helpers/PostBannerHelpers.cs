using Files.Enums;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System.Diagnostics;

namespace Files.Helpers
{
    public static class PostBannerHelpers
    {
        private static StatusCenterViewModel statusCenterViewModel => statusCenterViewModel;
        public static void PostBanner_Delete(ReturnResult status, FileOperationType operation, Stopwatch sw, IShellPage associatedInstance)
        {
            if (status == ReturnResult.Failed ||
                status == ReturnResult.UnknownException ||
                status == ReturnResult.IntegrityCheckFailed ||
                status == ReturnResult.AccessUnauthorized)
            {
                if (status == ReturnResult.AccessUnauthorized)
                {
                    statusCenterViewModel.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.IntegrityCheckFailed)
                {
                    statusCenterViewModel.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.Failed || status == ReturnResult.UnknownException)
                {
                    statusCenterViewModel.PostBanner(
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
                        statusCenterViewModel.PostBanner(
                        "StatusDeletionComplete".GetLocalized(),
                        "StatusOperationCompleted".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                    else if (operation == FileOperationType.Recycle)
                    {
                        statusCenterViewModel.PostBanner(
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