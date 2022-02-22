using System;
using Windows.UI.Xaml.Controls;
using Files.Backend.Models;
using Files.Backend.ViewModels.Dialogs;
using System.Threading.Tasks;
using Files.Shared.Enums;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
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
            ViewModel.ResultType = (e.ClickedItem as AddListItem).ItemType;
            this.Hide();
        }
    }
}