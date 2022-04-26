using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared;
using Files.Uwp.Extensions;
using System;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Files.Shared.Enums;
using Windows.UI.Xaml;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Uwp.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
    {
        public AddItemDialogViewModel ViewModel
        {
            get => (AddItemDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public AddItemDialog()
        {
            InitializeComponent();
        }

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.ResultType = (e.ClickedItem as AddItemDialogListItemViewModel).ItemResult;
            this.Hide();
        }

        private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var itemTypes = await ShellNewEntryExtensions.GetNewContextMenuEntries();
            await ViewModel.AddItemsToList(itemTypes); // TODO(i): This is a very cheap way of doing it, consider adding a service to retrieve the itemTypes list.
        }
    }
}