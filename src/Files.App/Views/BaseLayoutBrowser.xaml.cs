using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ServicesImplementation.Settings;
using Files.App.ViewModels;
using Files.Shared.Extensions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.App.Views.LayoutModes
{
    public sealed partial class BaseLayoutBrowser : UserControl
    {
        private readonly DispatcherQueueTimer tapDebounceTimer, dragOverTimer, hoverTimer;

        private LayoutModeViewModel? viewModel => DataContext as LayoutModeViewModel;
        private ListedItem? dragOverItem = null;
        private string? oldItemName = null;

        public BaseLayoutBrowser()
        {            
            InitializeComponent();

            PointerPressed += CoreWindow_PointerPressed;
            App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;
            PreviewKeyDown += ModernShellPage_PreviewKeyDown;
            dragOverTimer = DispatcherQueue.CreateTimer();
            tapDebounceTimer = base.DispatcherQueue.CreateTimer();
            hoverTimer = base.DispatcherQueue.CreateTimer();

            var flowDirectionSetting = /*
				TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
				Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
				replace the new instance created below with correct instance.
				Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			*/new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                base.FlowDirection = FlowDirection.RightToLeft;
            }
        }

        private void JumpTimer_Tick(object sender, object e)
        {
            jumpString = string.Empty;
            jumpTimer.Stop();
        }


        private ListedItem? preRenamingItem = null;

        public void CheckRenameDoubleClick(object clickedItem)
        {
            if (clickedItem is ListedItem item)
            {
                if (item == preRenamingItem)
                {
                    tapDebounceTimer.Debounce(() =>
                    {
                        if (item == preRenamingItem)
                        {
                            StartRenameItem();
                            tapDebounceTimer.Stop();
                        }
                    }, TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    tapDebounceTimer.Stop();
                    preRenamingItem = item;
                }
            }
            else
            {
                ResetRenameDoubleClick();
            }
        }

        private void StartRenameItem()
        {
            var renamingItem = viewModel.SelectedItems.FirstOrDefault();
            if (renamingItem == default(ListedItem))
            {
                return;
            }
            int extensionLength = renamingItem.FileExtension?.Length ?? 0;
            SelectorItem? listViewItem = BrowserControl.ContainerFromItem(renamingItem) as SelectorItem;
            if (listViewItem == null)
            {
                return;
            }
            TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
            TextBox? textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
            textBox.Text = textBlock?.Text;
            oldItemName = textBlock?.Text;
            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        public void ResetRenameDoubleClick()
        {
            preRenamingItem = null;
            tapDebounceTimer.Stop();
        }

        private async void ValidateItemNameInputText(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
        {
            if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
            {
                args.Cancel = true;
                await base.DispatcherQueue.EnqueueAsync(() =>
                {
                    var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
                    var oldText = textBox.Text;
                    textBox.Text = FilesystemHelpers.FilterRestrictedCharacters(args.NewText);
                    textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
                    showError?.Invoke(true);
                });
            }
            else
            {
                showError?.Invoke(false);
            }
        }

        private void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            viewModel.SelectedItems = e.Items.OfType<ListedItem>();
            
            try
            {
                // Only support IStorageItem capable paths
                var itemList = viewModel.SelectedItems.Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcutItem)).Select(x => VirtualStorageItem.FromListedItem(x));
                e.Data.SetStorageItems(itemList, false);
            }
            catch (Exception)
            {
                e.Cancel = true;
            }
        }

        private bool CanGetItemFromElement(object element)
            => element is SelectorItem;

        private ListedItem? GetItemFromElement(object element)
        {
            var item = element as ContentControl;
            if (item == null || !CanGetItemFromElement(element))
                return null;

            return item.DataContext as ListedItem ?? item.Content as ListedItem ?? BrowserControl.ItemFromContainer(item) as ListedItem;
        }

        private void OnItemDragLeave(object sender, DragEventArgs e)
        {
            var item = GetItemFromElement(sender);
            if (item == dragOverItem)
                dragOverItem = null; // Reset dragged over item
        }

        private async void OnItemDragOver(object sender, DragEventArgs e)
        {
            var item = GetItemFromElement(sender);
            if (item is null)
                return;

            DragOperationDeferral? deferral = null;
            try
            {
                deferral = e.GetDeferral();

                viewModel.SelectedItems = EnumerableExtensions.CreateEnumerable(item);

                if (dragOverItem != item)
                {
                    dragOverItem = item;
                    dragOverTimer.Stop();
                    dragOverTimer.Debounce(() =>
                    {
                        if (dragOverItem != null && !dragOverItem.IsExecutable)
                        {
                            dragOverItem = null;
                            dragOverTimer.Stop();
                            NavigationHelpers.OpenSelectedItems(viewModel.FilesystemViewModel.CurrentFolder, viewModel.SelectedItems, false);
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }

                if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
                {
                    e.Handled = true;

                    var handledByFtp = await FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
                    var draggedItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

                    if (draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else if (handledByFtp)
                    {
                        e.DragUIOverride.IsCaptionVisible = true;
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (!draggedItems.Any())
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else
                    {
                        e.DragUIOverride.IsCaptionVisible = true;
                        if (item.IsExecutable)
                        {
                            e.DragUIOverride.Caption = $"{"OpenItemsWithCaptionText".GetLocalizedResource()} {item.ItemName}";
                            e.AcceptedOperation = DataPackageOperation.Link;
                        } // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                        else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                        {
                            e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Link;
                        }
                        else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                        {
                            e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Copy;
                        }
                        else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                        {
                            e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Move;
                        }
                        else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                            || ZipStorageFolder.IsZipPath(item.ItemPath))
                        {
                            e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Copy;
                        }
                        else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
                        {
                            e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Move;
                        }
                        else
                        {
                            e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Copy;
                        }
                    }
                }
            }
            finally
            {
                deferral?.Complete();
            }
        }

        private async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            dragOverItem = null; // Reset dragged over item

            var item = GetItemFromElement(sender);
            if (item != null)
                await viewModel.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (item as ShortcutItem)?.TargetPath ?? item.ItemPath, false, true, item.IsExecutable);
            deferral.Complete();
        }

        /**
		 * Some keys are overriden by control built-in defaults (e.g. 'Space').
		 * They must be handled here since they're not propagated to KeyboardAccelerator.
		 */
        private async void OnPreviewKeyDown(object sender, KeyRoutedEventArgs args)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            
            switch (c: ctrl, s: shift, a: alt, t: true, k: args.Key)
            {
                case (true, false, false, true, (Windows.System.VirtualKey)192): // ctrl + ` (accent key), open terminal
                                                                  // Check if there is a folder selected, if not use the current directory.
                    string? path = viewModel.FilesystemViewModel.WorkingDirectory;
                    if (viewModel.SelectedItems.First().PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        path = viewModel.SelectedItems.FirstOrDefault()?.ItemPath;
                    }
                    await NavigationHelpers.OpenDirectoryInTerminal(path);
                    args.Handled = true;
                    break;

                case (false, false, false, true, Windows.System.VirtualKey.Space): // space, quick look
                    // handled in `CurrentPageType`::FileList_PreviewKeyDown
                    break;

                case (true, false, false, true, Windows.System.VirtualKey.Space): // ctrl + space, toggle media playback
                    if (PreviewPaneViewModel.PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
                    {
                        mediaPreviewContent.ViewModel.TogglePlayback();
                        args.Handled = true;
                    }
                    break;
            }
        }
    }
}
