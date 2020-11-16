using Microsoft.Toolkit.Uwp.Extensions;
using System.Diagnostics;

namespace Files.Helpers
{
    public static class PostBannerHelpers
    {
        public static void PostBanner_Delete(Status status, FileOperationType operation, Stopwatch sw, IShellPage associatedInstance)
        {
            if (status == Status.Failed ||
                status == Status.UnknownException ||
                status == Status.IntegrityCheckFailed ||
                status == Status.AccessUnauthorized)
            {
                if (status == Status.AccessUnauthorized)
                {
                    associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == Status.IntegrityCheckFailed)
                {
                    associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == Status.Failed || status == Status.UnknownException)
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
                        Status.Success,
                        operation);
                    }
                    else if (operation == FileOperationType.Recycle)
                    {
                        associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Recycle Complete",
                        "The operation has completed.",
                        0,
                        Status.Success,
                        operation);
                    }
                }
            }
        }
    }
}
