using Files.Enums;
using Microsoft.Toolkit.Uwp.Extensions;
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
                    associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.IntegrityCheckFailed)
                {
                    associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.Failed || status == ReturnResult.UnknownException)
                {
                    associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Deletion Failed",
                        "An unknown error has occured.",
                        0,
                        status,
                        operation);
                }
                else if (sw.Elapsed.TotalSeconds >= 10)
                {
                    if (operation == FileOperationType.Delete)
                    {
                        associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Deletion Complete",
                        "The operation has completed.",
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                    else if (operation == FileOperationType.Recycle)
                    {
                        associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Recycle Complete",
                        "The operation has completed.",
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                }
            }
        }
    }
}