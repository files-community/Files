using Files.Backend.Models;
using Files.Shared.Enums;
using Files.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class DecompressArchiveDialog : ContentDialog, IDialog<DecompressArchiveDialogViewModel>
    {
        public DecompressArchiveDialogViewModel ViewModel
        {
            get => (DecompressArchiveDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public DecompressArchiveDialog()
        {
            this.InitializeComponent();
        }

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
    }
}