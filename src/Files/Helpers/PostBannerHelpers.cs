using Files.Enums;
using Files.Filesystem;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Files.Helpers
{
    public static class PostBannerHelpers
    {
        private static OngoingTasksViewModel OngoingTasksViewModel => App.OngoingTasksViewModel;

        public static PostedStatusBanner PostBanner_Delete(IEnumerable<IStorageItemWithPath> source, ReturnResult returnStatus, bool permanently, bool canceled, int itemsDeleted)
        {
            var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);

            if (canceled)
            {
                if (permanently)
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusDeletionCancelled".GetLocalized(),
                        string.Format(source.Count() > 1 ?
                            itemsDeleted > 1 ? "StatusDeleteCanceledDetails_Plural".GetLocalized() : "StatusDeleteCanceledDetails_Plural2".GetLocalized()
                            : "StatusDeleteCanceledDetails_Singular".GetLocalized(), source.Count(), sourceDir, null, itemsDeleted),
                        0,
                        ReturnResult.Cancelled,
                        FileOperationType.Delete);
                }
                else
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusRecycleCancelled".GetLocalized(),
                        string.Format(source.Count() > 1 ?
                            itemsDeleted > 1 ? "StatusMoveCanceledDetails_Plural".GetLocalized() : "StatusMoveCanceledDetails_Plural2".GetLocalized()
                            : "StatusMoveCanceledDetails_Singular".GetLocalized(), source.Count(), sourceDir, "TheRecycleBin".GetLocalized(), itemsDeleted),
                        0,
                        ReturnResult.Cancelled,
                        FileOperationType.Recycle);
                }
            }
            else if (returnStatus == ReturnResult.InProgress)
            {
                if (permanently)
                {
                    // deleting items from <x>
                    return OngoingTasksViewModel.PostOperationBanner(string.Empty,
                        string.Format(source.Count() > 1 ? "StatusDeletingItemsDetails_Plural".GetLocalized() : "StatusDeletingItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir),
                        0,
                        ReturnResult.InProgress,
                        FileOperationType.Delete,
                        new CancellationTokenSource());
                }
                else
                {
                    // "Moving items from <x> to recycle bin"
                    return OngoingTasksViewModel.PostOperationBanner(string.Empty,
                        string.Format(source.Count() > 1 ? "StatusMovingItemsDetails_Plural".GetLocalized() : "StatusMovingItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir, "TheRecycleBin".GetLocalized()),
                        0,
                        ReturnResult.InProgress,
                        FileOperationType.Recycle,
                        new CancellationTokenSource());
                }
            }
            else if (returnStatus == ReturnResult.Success)
            {
                if (permanently)
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusDeletionComplete".GetLocalized(),
                        string.Format(source.Count() > 1 ? "StatusDeletedItemsDetails_Plural".GetLocalized() : "StatusDeletedItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir, itemsDeleted),
                        0,
                        ReturnResult.Success,
                        FileOperationType.Delete);
                }
                else
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusRecycleComplete".GetLocalized(),
                        string.Format(source.Count() > 1 ? "StatusMovedItemsDetails_Plural".GetLocalized() : "StatusMovedItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir, "TheRecycleBin".GetLocalized()),
                        0,
                        ReturnResult.Success,
                        FileOperationType.Recycle);
                }
            }
            else
            {
                if (permanently)
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusDeletionFailed".GetLocalized(),
                        string.Format(source.Count() > 1 ? "StatusDeletionFailedDetails_Plural".GetLocalized() : "StatusDeletionFailedDetails_Singular".GetLocalized(), source.Count(), sourceDir),
                        0,
                        ReturnResult.Failed,
                        FileOperationType.Delete);
                }
                else
                {
                    return OngoingTasksViewModel.PostBanner(
                        "StatusRecycleFailed".GetLocalized(),
                        string.Format(source.Count() > 1 ? "StatusMoveFailedDetails_Plural".GetLocalized() : "StatusMoveFailedDetails_Singular".GetLocalized(), source.Count(), sourceDir, "TheRecycleBin".GetLocalized()),
                        0,
                        ReturnResult.Failed,
                        FileOperationType.Recycle);
                }
            }
        }
    }
}