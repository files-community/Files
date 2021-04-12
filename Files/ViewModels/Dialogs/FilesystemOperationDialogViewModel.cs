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
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationDialogViewModel : ObservableObject
    {
        #region Public Properties

        public ObservableCollection<FilesystemOperationItemViewModel> Items { get; private set; }

        public ListViewSelectionMode ItemsSelectionMode
        {
            get => MustResolveConflicts ? ListViewSelectionMode.Extended : ListViewSelectionMode.None;
        }

        private string title;

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string subtitle;

        public string Subtitle
        {
            get => subtitle;
            set => SetProperty(ref subtitle, value);
        }

        private string primaryButtonText;

        public string PrimaryButtonText
        {
            get => primaryButtonText;
            set => SetProperty(ref primaryButtonText, value);
        }

        private string secondaryButtonText;

        public string SecondaryButtonText
        {
            get => secondaryButtonText;
            set => SetProperty(ref secondaryButtonText, value);
        }

        private bool chevronUpLoad = false;

        public bool ChevronUpLoad
        {
            get => chevronUpLoad;
            set => SetProperty(ref chevronUpLoad, value);
        }

        private bool chevronDownLoad = true;

        public bool ChevronDownLoad
        {
            get => chevronDownLoad;
            set => SetProperty(ref chevronDownLoad, value);
        }

        private bool expandableDetailsLoad = false;

        public bool ExpandableDetailsLoad
        {
            get => expandableDetailsLoad;
            set => SetProperty(ref expandableDetailsLoad, value);
        }

        private bool permanentlyDeleteLoad = false;

        public bool PermanentlyDeleteLoad
        {
            get => permanentlyDeleteLoad;
            set => SetProperty(ref permanentlyDeleteLoad, value);
        }

        private bool permanentlyDelete = false;

        public bool PermanentlyDelete
        {
            get => permanentlyDelete;
            set => SetProperty(ref permanentlyDelete, value);
        }

        private bool permanentlyDeleteEnabled = false;

        public bool PermanentlyDeleteEnabled
        {
            get => permanentlyDeleteEnabled;
            set => SetProperty(ref permanentlyDeleteEnabled, value);
        }

        private bool mustResolveConflicts = false;

        public bool MustResolveConflicts
        {
            get => mustResolveConflicts;
            set => SetProperty(ref mustResolveConflicts, value);
        }

        public IFilesystemOperationDialogView View { get; set; }

        #endregion Public Properties

        #region Commands

        public ICommand ExpandDetailsCommand { get; private set; }

        public ICommand OptionGenerateNewNameCommand { get; private set; }

        public ICommand OptionReplaceExistingCommand { get; private set; }

        public ICommand OptionSkipCommand { get; private set; }

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        public ICommand LoadedCommand { get; private set; }

        #endregion Commands

        public FilesystemOperationDialogViewModel()
        {
            // Create commands
            ExpandDetailsCommand = new RelayCommand(ExpandOrCollapseDetails);
            OptionGenerateNewNameCommand = new RelayCommand(OptionGenerateNewName);
            OptionReplaceExistingCommand = new RelayCommand(OptionReplaceExisting);
            OptionSkipCommand = new RelayCommand(OptionSkip);

            PrimaryButtonCommand = new RelayCommand(PrimaryButton);
            SecondaryButtonCommand = new RelayCommand(SecondaryButton);
            LoadedCommand = new RelayCommand(() => 
            {
                // Any conflicting items?
                if (MustResolveConflicts)
                {
                    ExpandOrCollapseDetails();
                }
            });
        }

        #region Command Implementation

        private void OptionSkip()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.ConflictResolveOption = FileNameConflictResolveOptionType.Skip;
            }
        }

        private void OptionReplaceExisting()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.ConflictResolveOption = FileNameConflictResolveOptionType.ReplaceExisting;
            }
        }

        private void OptionGenerateNewName()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName;
            }
        }

        private void PrimaryButton()
        {
            // Something there?
        }

        private void SecondaryButton()
        {
            if (MustResolveConflicts)
            {
                // Replace existing

                foreach (var item in Items)
                {
                    // Don't do anything
                    item.ConflictResolveOption = FileNameConflictResolveOptionType.Skip;
                }
            }
        }

        private void ExpandOrCollapseDetails()
        {
            bool detailsShown = !ExpandableDetailsLoad; // Inverted

            ExpandableDetailsLoad = detailsShown;
            ChevronDownLoad = !detailsShown;
            ChevronUpLoad = detailsShown;
        }

        #endregion Command Implementation

        public List<IFilesystemOperationItemModel> GetResult()
        {
            return Items.Cast<IFilesystemOperationItemModel>().ToList();
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
                PermanentlyDeleteLoad = permanentlyDeleteLoad,
                PermanentlyDelete = itemsData.PermanentlyDelete,
                PermanentlyDeleteEnabled = itemsData.PermanentlyDeleteEnabled,
                MustResolveConflicts = itemsData.MustResolveConflicts,
                Items = new ObservableCollection<FilesystemOperationItemViewModel>(itemsData.ToItems())
            };

            FilesystemOperationDialog dialog = new FilesystemOperationDialog(viewModel);

            return dialog;
        }
    }
}