using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Extensions;
using Files.Backend.Services;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogViewModel : BaseDialogViewModel
    {
        private readonly CancellationTokenSource _dialogClosingCts;

        public ObservableCollection<BaseFileSystemDialogItemViewModel> Items { get; }

        public FileSystemDialogMode FileSystemDialogMode { get; }

        private string? _Description;
        public string? Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private bool _DeletePermanently;
        public bool DeletePermanently
        {
            get => _DeletePermanently;
            set => SetProperty(ref _DeletePermanently, value);
        }

        private bool _IsDeletePermanentlyEnabled;
        public bool IsDeletePermanentlyEnabled
        {
            get => _IsDeletePermanentlyEnabled;
            set => SetProperty(ref _IsDeletePermanentlyEnabled, value);
        }

        internal FileSystemDialogViewModel(FileSystemDialogMode fileSystemDialogMode, IEnumerable<BaseFileSystemDialogItemViewModel> items)
        {
            this.FileSystemDialogMode = fileSystemDialogMode;
            _dialogClosingCts = new();
            Items = new(items);

            PrimaryButtonClickCommand = new RelayCommand(PrimaryButtonClick);
            SecondaryButtonClickCommand = new RelayCommand(SecondaryButtonClick);
        }

        public void ApplyConflictOptionToAll(FileNameConflictResolveOptionType e)
        {
            if (!FileSystemDialogMode.IsInDeleteMode)
            {
                foreach (var item in Items)
                {
                    if (item is FileSystemDialogConflictItemViewModel conflictItem && !conflictItem.IsActionTaken)
                    {
                        conflictItem.TakeAction(e);
                    }
                }

                PrimaryButtonEnabled = true;
            }
        }

        public IEnumerable<IFileSystemDialogConflictItemViewModel> GetItemsResult()
        {
            return Items.Cast<IFileSystemDialogConflictItemViewModel>();
        }

        public void CancelCts()
        {
            _dialogClosingCts.Cancel();
            _dialogClosingCts.Dispose();
        }

        private void PrimaryButtonClick()
        {
            if (!FileSystemDialogMode.IsInDeleteMode)
            {
                ApplyConflictOptionToAll(FileNameConflictResolveOptionType.GenerateNewName);
            }
        }

        private void SecondaryButtonClick()
        {
            if (FileSystemDialogMode.ConflictsExist)
            {
                foreach (var item in Items)
                {
                    // Don't do anything, skip
                    if (item is FileSystemDialogConflictItemViewModel conflictItem)
                    {
                        conflictItem.ConflictResolveOption = FileNameConflictResolveOptionType.Skip;
                    }
                }
            }
        }

        public static FileSystemDialogViewModel GetDialogViewModel(FileSystemDialogMode dialogMode, (bool deletePermanently, bool IsDeletePermanentlyEnabled) deleteOption, FilesystemOperationType operationType, List<BaseFileSystemDialogItemViewModel> nonConflictingItems, List<BaseFileSystemDialogItemViewModel> conflictingItems)
        {
            string titleText = string.Empty;
            string descriptionText = string.Empty;
            string primaryButtonText = string.Empty;
            string secondaryButtonText = string.Empty;
            bool isInDeleteMode = false;

            if (dialogMode.ConflictsExist)
            {
                // Subtitle text
                if (conflictingItems.Count > 1)
                {
                    if (nonConflictingItems.Count > 0)
                    {
                        // There are {0} conflicting file names, and {1} outgoing item(s)
                        descriptionText = string.Format("ConflictingItemsDialogSubtitleMultipleConflictsMultipleNonConflicts".ToLocalized(), conflictingItems.Count, nonConflictingItems.Count);
                    }
                    else
                    {
                        // There are {0} conflicting file names
                        descriptionText = string.Format("ConflictingItemsDialogSubtitleMultipleConflictsNoNonConflicts".ToLocalized(), conflictingItems.Count);
                    }
                }
                else
                {
                    if (nonConflictingItems.Count > 0)
                    {
                        // There is one conflicting file name, and {0} outgoing item(s)
                        descriptionText = string.Format("ConflictingItemsDialogSubtitleSingleConflictMultipleNonConflicts".ToLocalized(), nonConflictingItems.Count);
                    }
                    else
                    {
                        // There is one conflicting file name
                        descriptionText = string.Format("ConflictingItemsDialogSubtitleSingleConflictNoNonConflicts".ToLocalized(), conflictingItems.Count);
                    }
                }

                titleText = "ConflictingItemsDialogTitle".ToLocalized();
                primaryButtonText = "ConflictingItemsDialogPrimaryButtonText".ToLocalized();
                secondaryButtonText = "Cancel".ToLocalized();
            }
            else
            {
                switch (operationType)
                {
                    case FilesystemOperationType.Copy:
                        {
                            titleText = "CopyItemsDialogTitle".ToLocalized();
                            descriptionText = nonConflictingItems.Count + conflictingItems.Count == 1 ? "CopyItemsDialogSubtitleSingle".ToLocalized() : string.Format("CopyItemsDialogSubtitleMultiple".ToLocalized(), nonConflictingItems.Count + conflictingItems.Count);
                            primaryButtonText = "Copy".ToLocalized();
                            secondaryButtonText = "Cancel".ToLocalized();
                            break;
                        }

                    case FilesystemOperationType.Move:
                        {
                            titleText = "MoveItemsDialogTitle".ToLocalized();
                            descriptionText = nonConflictingItems.Count + conflictingItems.Count == 1 ? "MoveItemsDialogSubtitleSingle".ToLocalized() : string.Format("MoveItemsDialogSubtitleMultiple".ToLocalized(), nonConflictingItems.Count + conflictingItems.Count);
                            primaryButtonText = "MoveItemsDialogPrimaryButtonText".ToLocalized();
                            secondaryButtonText = "Cancel".ToLocalized();
                            break;
                        }

                    case FilesystemOperationType.Delete:
                        {
                            titleText = "DeleteItemsDialogTitle".ToLocalized();
                            descriptionText = nonConflictingItems.Count + conflictingItems.Count == 1 ? "DeleteItemsDialogSubtitleSingle".ToLocalized() : string.Format("DeleteItemsDialogSubtitleMultiple".ToLocalized(), nonConflictingItems.Count);
                            primaryButtonText = "Delete".ToLocalized();
                            secondaryButtonText = "Cancel".ToLocalized();
                            isInDeleteMode = true;
                            break;
                        }
                }
            }

            var viewModel = new FileSystemDialogViewModel(new() { IsInDeleteMode = isInDeleteMode, ConflictsExist = !conflictingItems.IsEmpty() }, conflictingItems.Concat(nonConflictingItems))
            {
                Title = titleText,
                Description = descriptionText,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                DeletePermanently = deleteOption.deletePermanently,
                IsDeletePermanentlyEnabled = deleteOption.IsDeletePermanentlyEnabled
            };

            _ = LoadItemsIcon(viewModel.Items, viewModel._dialogClosingCts.Token);

            return viewModel;
        }

        private static async Task LoadItemsIcon(IEnumerable<BaseFileSystemDialogItemViewModel> items, CancellationToken token)
        {
            var imagingService = Ioc.Default.GetRequiredService<IImagingService>();
            var threadingService = Ioc.Default.GetRequiredService<IThreadingService>();

            await items.ParallelForEach(async (item) =>
            {
                try
                {
                    if (token.IsCancellationRequested) return;

                    await threadingService.ExecuteOnUiThreadAsync(async () =>
                    {
                        item.ItemIcon = await imagingService.GetImageModelFromPathAsync(item.SourcePath!, 64u);
                    });
                }
                catch (Exception ex) { }
            }, 10, token);
        }
    }

    public sealed class FileSystemDialogMode
    {
        public bool IsInDeleteMode { get; init; }

        public bool ConflictsExist { get; init; }
    }
}
