using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Files.Shared.Enums;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Files.Uwp.Helpers.XamlHelpers;
using Windows.UI.Core;
using Files.Uwp.Filesystem;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Uwp.Dialogs
{
    public sealed partial class FilesystemOperationDialog : ContentDialog, IDialog<FileSystemDialogViewModel>
    {
        public FileSystemDialogViewModel ViewModel
        {
            get => (FileSystemDialogViewModel)DataContext;
            set
            {
                if (value != null)
                {
                    value.PrimaryButtonEnabled = true;
                }

                DataContext = value;
            }
        }

        public FilesystemOperationDialog()
        {
            this.InitializeComponent();

            Window.Current.SizeChanged += Current_SizeChanged;
        }

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateDialogLayout();
        }

        private void UpdateDialogLayout()
        {
            if (ViewModel.FileSystemDialogMode.ConflictsExist)
            {
                if (Window.Current.Bounds.Width <= 700)
                {
                    ContainerGrid.Width = Window.Current.Bounds.Width - 50;
                }
                else
                {
                    ContainerGrid.Width = 650;
                }
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var primaryButton = this.FindDescendant("PrimaryButton") as Button;
            if (primaryButton != null)
            {
                primaryButton.GotFocus += PrimaryButton_GotFocus;
            }
        }

        private void PrimaryButton_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as Button).GotFocus -= PrimaryButton_GotFocus;
            if (chkPermanentlyDelete != null)
            {
                chkPermanentlyDelete.IsEnabled = ViewModel.IsDeletePermanentlyEnabled;
            }
            DetailsGrid.IsEnabled = true;
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var t = (sender as MenuFlyoutItem).Tag as string;
            if (t == "All")
            {
                if (DetailsGrid.SelectedItems.FirstOrDefault() is FileSystemDialogConflictItemViewModel conflictItem)
                {
                    ViewModel.ApplyConflictOptionToAll(conflictItem.ConflictResolveOption);
                }

                return;
            }

            var op = (FileNameConflictResolveOptionType)int.Parse(t);
            foreach (var item in DetailsGrid.SelectedItems)
            {
                if (item is FileSystemDialogConflictItemViewModel conflictItem)
                {
                    conflictItem.ConflictResolveOption = op;
                }
            }
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            if (!ViewModel.FileSystemDialogMode.ConflictsExist)
            {
                return;
            }

            if (((sender as MenuFlyout)?.Target as ListViewItem)?.Content is BaseFileSystemDialogItemViewModel li)
            {
                if (!DetailsGrid.SelectedItems.Contains(li))
                {
                    DetailsGrid.SelectedItems.Add(li);
                }
            }

            if (DetailsGrid.Items.Count > 1 && DetailsGrid.SelectedItems.Count == 1 && !DetailsGrid.SelectedItems.Any(x => (x as FileSystemDialogConflictItemViewModel).IsDefault))
            {
                ApplyToAllOption.Visibility = Visibility.Visible;
                ApplyToAllSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                ApplyToAllOption.Visibility = Visibility.Collapsed;
                ApplyToAllSeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void RootDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
            ViewModel.CancelCts();
        }

        private void NameStackPanel_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.DataContext is FileSystemDialogConflictItemViewModel conflictItem
                && conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName)
            {
                conflictItem.IsTextBoxVisible = conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName;
                conflictItem.CustomName = conflictItem.DestinationDisplayName;
            }
        }

        private void NameEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FileSystemDialogConflictItemViewModel conflictItem)
            {
                conflictItem.CustomName = FilesystemHelpers.FilterRestrictedCharacters(conflictItem.CustomName);

                if (ViewModel.IsNameAvailableForItem(conflictItem, conflictItem.CustomName!))
                {
                    conflictItem.IsTextBoxVisible = false;
                }
                else
                {
                    ViewModel.PrimaryButtonEnabled = false;
                }

                if (conflictItem.CustomName.Equals(conflictItem.DisplayName))
                {
                    var savedName = conflictItem.DestinationDisplayName;
                    conflictItem.CustomName = string.Empty;
                    conflictItem.DestinationDisplayName = savedName;
                }
            }
        }

        private void NameEdit_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.Focus(FocusState.Programmatic);
        }

        private void FilesystemOperationDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (ViewModel.FileSystemDialogMode.IsInDeleteMode)
            {
                DescriptionText.Foreground = App.Current.Resources["TextControlForeground"] as SolidColorBrush;
            }

            UpdateDialogLayout();
        }
    }
}