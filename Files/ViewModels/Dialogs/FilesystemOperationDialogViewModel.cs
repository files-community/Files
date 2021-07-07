﻿using Files.DataModels;
using Files.Dialogs;
using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationDialogViewModel : ObservableObject
    {

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

        private bool primaryButtonEnabled = false;

        public bool PrimaryButtonEnabled
        {
            get => primaryButtonEnabled;
            set => SetProperty(ref primaryButtonEnabled, value);
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

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        public ICommand LoadedCommand { get; private set; }

        public FilesystemOperationDialogViewModel()
        {
            // Create commands
            PrimaryButtonCommand = new RelayCommand(PrimaryButton);
            SecondaryButtonCommand = new RelayCommand(SecondaryButton);
            LoadedCommand = new RelayCommand(() =>
            {
                UpdatePrimaryButtonEnabled();
            });
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

        public void OptionSkip()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.TakeAction(FileNameConflictResolveOptionType.Skip);
            }
        }

        public void OptionReplaceExisting()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.TakeAction(FileNameConflictResolveOptionType.ReplaceExisting);
            }
        }

        public void OptionGenerateNewName()
        {
            foreach (var item in View.SelectedItems)
            {
                var detailItem = (FilesystemOperationItemViewModel)item;

                detailItem.TakeAction(FileNameConflictResolveOptionType.GenerateNewName);
            }
        }

        public void UpdatePrimaryButtonEnabled()
        {
            if (MustResolveConflicts)
            {
                PrimaryButtonEnabled = !Items.Any((item) => !item.ActionTaken);
            }
            else if (PermanentlyDeleteLoad) // PermanentlyDeleteLoad - is only loaded (`true`) when deleting items
            {
                PrimaryButtonEnabled = true;
            }
        }

        public List<IFilesystemOperationItemModel> GetResult()
        {
            return Items.Cast<IFilesystemOperationItemModel>().ToList();
        }

        public static async Task<FilesystemOperationDialog> GetDialog(FilesystemItemsOperationDataModel itemsData)
        {
            string titleText = null;
            string subtitleText = null;
            string primaryButtonText = null;
            string secondaryButtonText = null;
            bool permanentlyDeleteLoad = false;

            if (itemsData.MustResolveConflicts)
            {
                List<FilesystemItemsOperationItemModel> nonConflictingItems = itemsData.IncomingItems.Except(itemsData.ConflictingItems).ToList();

                // Subtitle text
                if (itemsData.ConflictingItems.Count > 1)
                {
                    if (nonConflictingItems.Count > 0)
                    {
                        // There are {0} conflicting file names, and {1} outgoing item(s)
                        subtitleText = string.Format("ConflictingItemsDialogSubtitleMultipleConflictsMultipleNonConflicts".GetLocalized(), itemsData.ConflictingItems.Count, nonConflictingItems.Count);
                    }
                    else
                    {
                        // There are {0} conflicting file names
                        subtitleText = string.Format("ConflictingItemsDialogSubtitleMultipleConflictsNoNonConflicts".GetLocalized(), itemsData.ConflictingItems.Count);
                    }
                }    
                else
                {
                    if (nonConflictingItems.Count > 0)
                    {
                        // There is one conflicting file name, and {0} outgoing item(s)
                        subtitleText = string.Format("ConflictingItemsDialogSubtitleSingleConflictMultipleNonConflicts".GetLocalized(), nonConflictingItems.Count);
                    }
                    else
                    {
                        // There is one conflicting file name
                        subtitleText = string.Format("ConflictingItemsDialogSubtitleSingleConflictNoNonConflicts".GetLocalized(), itemsData.ConflictingItems.Count);
                    }
                }

                titleText = "ConflictingItemsDialogTitle".GetLocalized();
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
                MustResolveConflicts = itemsData.MustResolveConflicts
            };
            viewModel.Items = new ObservableCollection<FilesystemOperationItemViewModel>(await itemsData.ToItems(
                viewModel.UpdatePrimaryButtonEnabled, viewModel.OptionGenerateNewName, viewModel.OptionReplaceExisting, viewModel.OptionSkip));

            FilesystemOperationDialog dialog = new FilesystemOperationDialog(viewModel);

            return dialog;
        }
    }
}