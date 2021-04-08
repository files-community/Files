using Files.DataModels;
using Files.Dialogs;
using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationDialogViewModel : ObservableObject
    {
        private bool chevronDownLoad = true;
        private bool chevronUpLoad = false;
        private string closeButtonText;
        private bool expandableDetailsLoad = false;
        private bool mustResolveConflicts = false;
        private bool permanentlyDelete = false;
        private bool permanentlyDeleteEnabled = false;
        private bool permanentlyDeleteLoad = false;
        private string primaryButtonText;
        private string secondaryButtonText;
        private string subtitle;
        private string title;

        public FilesystemOperationDialogViewModel()
        {
            // Create commands
            PrimaryButtonCommand = new RelayCommand(PrimaryButton);
            SecondaryButtonCommand = new RelayCommand(SecondaryButton);
            CloseButtonCommand = new RelayCommand(CloseButton);
        }

        public bool ChevronDownLoad
        {
            get => chevronDownLoad;
            set => SetProperty(ref chevronDownLoad, value);
        }

        public bool ChevronUpLoad
        {
            get => chevronUpLoad;
            set => SetProperty(ref chevronUpLoad, value);
        }

        public ICommand CloseButtonCommand { get; private set; }

        public string CloseButtonText
        {
            get => closeButtonText;
            set => SetProperty(ref closeButtonText, value);
        }

        public bool ExpandableDetailsLoad
        {
            get => expandableDetailsLoad;
            set => SetProperty(ref expandableDetailsLoad, value);
        }

        public ICommand ExpandDetailsCommand { get; private set; }
        public ObservableCollection<FilesystemOperationItemViewModel> Items { get; private set; }

        public bool MustResolveConflicts
        {
            get => mustResolveConflicts;
            set => SetProperty(ref mustResolveConflicts, value);
        }

        public bool PermanentlyDelete
        {
            get => permanentlyDelete;
            set => SetProperty(ref permanentlyDelete, value);
        }

        public bool PermanentlyDeleteEnabled
        {
            get => permanentlyDeleteEnabled;
            set => SetProperty(ref permanentlyDeleteEnabled, value);
        }

        public bool PermanentlyDeleteLoad
        {
            get => permanentlyDeleteLoad;
            set => SetProperty(ref permanentlyDeleteLoad, value);
        }

        public ICommand PrimaryButtonCommand { get; private set; }

        public string PrimaryButtonText
        {
            get => primaryButtonText;
            set => SetProperty(ref primaryButtonText, value);
        }

        public ICommand SecondaryButtonCommand { get; private set; }

        public string SecondaryButtonText
        {
            get => secondaryButtonText;
            set => SetProperty(ref secondaryButtonText, value);
        }

        public string Subtitle
        {
            get => subtitle;
            set => SetProperty(ref subtitle, value);
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public static FilesystemOperationDialog GetDialog(FilesystemItemsOperationDataModel itemsData)
        {
            string titleText = null;
            string subtitleText = null;
            string primaryButtonText = null;
            string secondaryButtonText = null;
            string closeButtonText = null;
            bool permanentlyDeleteLoad = false;

            if (itemsData.MustResolveConflicts)
            {
                List<FilesystemItemsOperationItemModel> nonConflictingItems = itemsData.IncomingItems.Except(itemsData.ConflictingItems).ToList();

                titleText = "ConflictingItemsDialogTitle".GetLocalized();
                subtitleText = itemsData.ConflictingItems.Count == 1 ? string.Format("ConflictingItemsDialogSubtitleSingle".GetLocalized(), nonConflictingItems.Count) : string.Format("ConflictingItemsDialogSubtitleMultiple".GetLocalized(), itemsData.ConflictingItems.Count, nonConflictingItems.Count);
                primaryButtonText = "ConflictingItemsDialogPrimaryButtonText".GetLocalized();
                secondaryButtonText = "ConflictingItemsDialogSecondaryButtonText".GetLocalized();
                closeButtonText = "ConflictingItemsDialogCloseButtonText".GetLocalized();
            }
            else
            {
                switch (itemsData.OperationType)
                {
                    case FilesystemOperationType.Copy:
                        {
                            titleText = "CopyItemsDialogTitle".GetLocalized();
                            subtitleText = itemsData.IncomingItems.Count == 1 ? "CopyItemsDialogSubtitleSingle".GetLocalized() : string.Format("CopyItemsDialogSubtitleMultiple".GetLocalized(), itemsData.IncomingItems.Count);
                            primaryButtonText = "CopyItemsDialogPrimaryButtonText".GetLocalized();
                            secondaryButtonText = "CopyItemsDialogSecondaryButtonText".GetLocalized();
                            break;
                        }

                    case FilesystemOperationType.Move:
                        {
                            titleText = "MoveItemsDialogTitle".GetLocalized();
                            subtitleText = itemsData.IncomingItems.Count == 1 ? "MoveItemsDialogSubtitleSingle".GetLocalized() : string.Format("MoveItemsDialogSubtitleMultiple".GetLocalized(), itemsData.IncomingItems.Count);
                            primaryButtonText = "MoveItemsDialogPrimaryButtonText".GetLocalized();
                            secondaryButtonText = "MoveItemsDialogSecondaryButtonText".GetLocalized();
                            break;
                        }

                    case FilesystemOperationType.Delete:
                        {
                            titleText = "DeleteItemsDialogTitle".GetLocalized();
                            subtitleText = itemsData.IncomingItems.Count == 1 ? "DeleteItemsDialogSubtitleSingle".GetLocalized() : string.Format("DeleteItemsDialogSubtitleMultiple".GetLocalized(), itemsData.IncomingItems.Count);
                            primaryButtonText = "DeleteItemsDialogPrimaryButtonText".GetLocalized();
                            secondaryButtonText = "DeleteItemsDialogSecondaryButtonText".GetLocalized();
                            permanentlyDeleteLoad = true;
                            break;
                        }
                }
            }

            FilesystemOperationDialogViewModel viewModel = new FilesystemOperationDialogViewModel()
            {
                Title = titleText,
                Subtitle = subtitleText,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                CloseButtonText = closeButtonText,
                PermanentlyDeleteLoad = permanentlyDeleteLoad,
                PermanentlyDelete = itemsData.PermanentlyDelete,
                PermanentlyDeleteEnabled = itemsData.PermanentlyDeleteEnabled,
                MustResolveConflicts = itemsData.MustResolveConflicts,
                ExpandDetailsCommand = new RelayCommand<FilesystemOperationDialogViewModel>((vm) =>
                {
                    bool detailsShown = !vm.ExpandableDetailsLoad; // Inverted

                    vm.ExpandableDetailsLoad = detailsShown;
                    vm.ChevronDownLoad = !detailsShown;
                    vm.ChevronUpLoad = detailsShown;
                }),
                Items = new ObservableCollection<FilesystemOperationItemViewModel>(itemsData.ToItems())
            };

            FilesystemOperationDialog dialog = new FilesystemOperationDialog(viewModel);

            return dialog;
        }

        public List<IFilesystemOperationItemModel> GetResult()
        {
            return Items.Cast<IFilesystemOperationItemModel>().ToList();
        }

        private void CloseButton()
        {
            if (MustResolveConflicts)
            {
                // Skip

                foreach (var item in Items)
                {
                    item.ConflictResolveOption = FileNameConflictResolveOptionType.Skip;
                }
            }
        }

        private void PrimaryButton()
        {
            if (MustResolveConflicts)
            {
                // Generate new name

                foreach (var item in Items)
                {
                    item.ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName;
                }
            }
        }

        private void SecondaryButton()
        {
            if (MustResolveConflicts)
            {
                // Replace existing

                foreach (var item in Items)
                {
                    item.ConflictResolveOption = FileNameConflictResolveOptionType.ReplaceExisting;
                }
            }
        }
    }
}