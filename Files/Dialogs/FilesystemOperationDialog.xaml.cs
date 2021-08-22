using Files.Enums;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class FilesystemOperationDialog : ContentDialog, IFilesystemOperationDialogView
    {
        public FilesystemOperationDialogViewModel ViewModel
        {
            get => (FilesystemOperationDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public IList<object> SelectedItems => DetailsGrid.SelectedItems;

        public FilesystemOperationDialog(FilesystemOperationDialogViewModel viewModel)
        {
            this.InitializeComponent();

            ViewModel = viewModel;
            ViewModel.View = this;
        }

        private void MenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var t = (sender as MenuFlyoutItem).Tag as string;
            if(t == "All")
            {
                var i = DetailsGrid.SelectedItems.FirstOrDefault() as FilesystemOperationItemViewModel;
                if(i is not null)
                {
                    ViewModel.ApplyConflictOptionToAll(i.ConflictResolveOption);
                }
                return;
            }

            var op = (FileNameConflictResolveOptionType)int.Parse(t);
            foreach (var item in DetailsGrid.SelectedItems)
            {
                if(item is FilesystemOperationItemViewModel model)
                {
                    model.TakeAction(op);
                }
            }
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            if(!ViewModel.MustResolveConflicts)
            {
                return;
            }

            if (((sender as MenuFlyout)?.Target as ListViewItem)?.Content is FilesystemOperationItemViewModel li)
            {
                if(!DetailsGrid.SelectedItems.Contains(li))
                {
                    DetailsGrid.SelectedItems.Add(li);
                }
            }

            if (DetailsGrid.Items.Count > 1 && DetailsGrid.SelectedItems.Count == 1 && DetailsGrid.SelectedItems.Any(x => (x as FilesystemOperationItemViewModel).ActionTaken))
            {
                ApplyToAllOption.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ApplyToAllSeparator.Visibility = Windows.UI.Xaml.Visibility.Visible;
            } 
            else
            {
                ApplyToAllOption.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                ApplyToAllSeparator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void Grid_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // if there are conflicts to be resolved, apply the conflict context flyout
            if(ViewModel.MustResolveConflicts)
            {
                (sender as Grid).FindAscendant<ListViewItem>().ContextFlyout = ItemContextFlyout;
            }
        }
    }

    public interface IFilesystemOperationDialogView
    {
        IList<object> SelectedItems { get; }
    }
}