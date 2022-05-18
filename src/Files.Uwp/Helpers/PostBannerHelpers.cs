using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Files.Uwp.Helpers
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

        public static PostedStatusBanner PostBanner_Copy(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCopied)
        {
            var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
            var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

            if (canceled)
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusCopyCanceled".GetLocalized(),
                    string.Format(source.Count() > 1 ?
                        itemsCopied > 1 ? "StatusCopyCanceledDetails_Plural".GetLocalized() : "StatusCopyCanceledDetails_Plural2".GetLocalized() :
                        "StatusCopyCanceledDetails_Singular".GetLocalized(), source.Count(), destinationDir, itemsCopied),
                    0,
                    ReturnResult.Cancelled,
                    FileOperationType.Copy);
            }
            else if (returnStatus == ReturnResult.InProgress)
            {
                return OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    string.Format(source.Count() > 1 ? "StatusCopyingItemsDetails_Plural".GetLocalized() : "StatusCopyingItemsDetails_Singular".GetLocalized(), source.Count(), destinationDir),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Copy, new CancellationTokenSource());
            }
            else if (returnStatus == ReturnResult.Success)
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusCopyComplete".GetLocalized(),
                    string.Format(source.Count() > 1 ? "StatusCopiedItemsDetails_Plural".GetLocalized() : "StatusCopiedItemsDetails_Singular".GetLocalized(), source.Count(), destinationDir, itemsCopied),
                    0,
                    ReturnResult.Success,
                    FileOperationType.Copy);
            }
            else
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusCopyFailed".GetLocalized(),
                    string.Format(source.Count() > 1 ? "StatusCopyFailedDetails_Plural".GetLocalized() : "StatusCopyFailedDetails_Singular".GetLocalized(), source.Count(), sourceDir, destinationDir),
                    0,
                    ReturnResult.Failed,
                    FileOperationType.Copy);
            }
        }

        public static PostedStatusBanner PostBanner_Move(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsMoved)
        {
            var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
            var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

            if (canceled)
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusMoveCanceled".GetLocalized(),
                    string.Format(source.Count() > 1 ?
                        itemsMoved > 1 ? "StatusMoveCanceledDetails_Plural".GetLocalized() : "StatusMoveCanceledDetails_Plural2".GetLocalized()
                        : "StatusMoveCanceledDetails_Singular".GetLocalized(), source.Count(), sourceDir, destinationDir, itemsMoved),
                    0,
                    ReturnResult.Cancelled,
                    FileOperationType.Move);
            }
            else if (returnStatus == ReturnResult.InProgress)
            {
                return OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    string.Format(source.Count() > 1 ? "StatusMovingItemsDetails_Plural".GetLocalized() : "StatusMovingItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir, destinationDir),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Move, new CancellationTokenSource());
            }
            else if (returnStatus == ReturnResult.Success)
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusMoveComplete".GetLocalized(),
                    string.Format(source.Count() > 1 ? "StatusMovedItemsDetails_Plural".GetLocalized() : "StatusMovedItemsDetails_Singular".GetLocalized(), source.Count(), sourceDir, destinationDir, itemsMoved),
                    0,
                    ReturnResult.Success,
                    FileOperationType.Move);
            }
            else
            {
                return OngoingTasksViewModel.PostBanner(
                    "StatusMoveFailed".GetLocalized(),
                    string.Format(source.Count() > 1 ? "StatusMoveFailedDetails_Plural".GetLocalized() : "StatusMoveFailedDetails_Singular".GetLocalized(), source.Count(), sourceDir, destinationDir),
                    0,
                    ReturnResult.Failed,
                    FileOperationType.Move);
            }
        }
    }
}