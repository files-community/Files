using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Files.Shared.Enums;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Uwp.Helpers.XamlHelpers;

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
        }

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

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
            ViewModel.CancelCts();
        }

        private void NameStackPanel_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.DataContext is FileSystemDialogConflictItemViewModel conflictItem)
            {
                conflictItem.IsTextBoxVisible = conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName;
                conflictItem.CustomName = conflictItem.DestinationDisplayName;

                var textBox = DependencyObjectHelpers.FindChild<TextBox>(element);
                textBox?.Focus(FocusState.Programmatic);
            }
        }

        private void NameEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FileSystemDialogConflictItemViewModel conflictItem)
            {
                if (ViewModel.IsNameAvailableForItem(conflictItem, conflictItem.CustomName!))
                {
                    conflictItem.IsTextBoxVisible = false;
                }
                else
                {
                    ViewModel.PrimaryButtonEnabled = false;
                }
            }
        }
    }
}