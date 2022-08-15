using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared;
using Files.Uwp.Extensions;
using System;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;

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

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await this.SetContentDialogRoot(this).ShowAsync();

        // WINUI3
        private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            }
            return contentDialog;
        }

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