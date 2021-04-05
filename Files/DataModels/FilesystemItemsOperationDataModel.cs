using Files.Enums;
using Files.ViewModels.Dialogs;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

namespace Files.DataModels
{
    public struct FilesystemItemsOperationItemModel
    {
        public FilesystemOperationType OperationType;

        public string SourcePath;

        public string DestinationPath;

        public FilesystemItemsOperationItemModel(FilesystemOperationType operationType, string sourcePath, string destinationPath)
        {
            this.OperationType = operationType;
            this.SourcePath = sourcePath;
            this.DestinationPath = destinationPath;
        }
    }

    public struct FilesystemItemsOperationDataModel
    {
        public FilesystemOperationType OperationType;

        public bool MustResolveConflicts;

        public bool PermanentlyDelete;

        public bool PermanentlyDeleteEnabled;

        /// <summary>
        /// The items that are copied/moved/deleted from the source directory (to destination)
        /// </summary>
        public List<FilesystemItemsOperationItemModel> IncomingItems;

        /// <summary>
        /// The items that are conflicting between <see cref="IncomingItems"/> and the items that are in the destination directory
        /// </summary>
        public List<FilesystemItemsOperationItemModel> ConflictingItems;

        public FilesystemItemsOperationDataModel(FilesystemOperationType operationType, bool mustResolveConflicts, bool permanentlyDelete, bool permanentlyDeleteEnabled, List<FilesystemItemsOperationItemModel> incomingItems, List<FilesystemItemsOperationItemModel> conflictingItems)
        {
            this.OperationType = operationType;
            this.MustResolveConflicts = mustResolveConflicts;
            this.PermanentlyDelete = permanentlyDelete;
            this.PermanentlyDeleteEnabled = permanentlyDeleteEnabled;
            this.IncomingItems = incomingItems;
            this.ConflictingItems = conflictingItems;
        }

        public List<FilesystemOperationItemViewModel> ToItems()
        {
            List<FilesystemOperationItemViewModel> items = new List<FilesystemOperationItemViewModel>();

            List<FilesystemItemsOperationItemModel> nonConflictingItems = IncomingItems.Except(ConflictingItems).ToList();

            // Add conflicting items first
            foreach (var item in ConflictingItems)
            {
                items.Add(new FilesystemOperationItemViewModel()
                {
                    OperationIconGlyph = GetOperationIconGlyph(item.OperationType),
                    SourcePath = item.SourcePath,
                    PlusIconVisibility = Visibility.Collapsed,
                    DestinationPath = item.DestinationPath,
                    IsConflicting = true,
                    ExclamationMarkVisibility = Visibility.Visible,
                    ItemOperation = item.OperationType
                });
            }

            // Then add non-conflicting items
            foreach (var item in nonConflictingItems)
            {
                items.Add(new FilesystemOperationItemViewModel()
                {
                    OperationIconGlyph = GetOperationIconGlyph(item.OperationType),
                    SourcePath = item.SourcePath,
                    ArrowIconVisibility = item.OperationType == FilesystemOperationType.Delete ? Visibility.Collapsed : Visibility.Visible,
                    PlusIconVisibility = item.OperationType == FilesystemOperationType.Delete ? Visibility.Collapsed : Visibility.Visible,
                    DestinationPath = item.DestinationPath,
                    IsConflicting = false,
                    ExclamationMarkVisibility = Visibility.Collapsed,
                    ItemOperation = item.OperationType
                });
            }

            return items;
        }

        private string GetOperationIconGlyph(FilesystemOperationType operationType)
        {
            switch (operationType)
            {
                case FilesystemOperationType.Copy:
                    return "\uE8C8";

                case FilesystemOperationType.Move:
                    return "\uE8C6";

                case FilesystemOperationType.Delete:
                    return "\uE74D";

                default:
                    return "\uE8FB";
            }
        }
    }
}
