using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Uwp.Dialogs
{
    public sealed partial class ElevateConfirmDialog : ContentDialog, IDialog<ElevateConfirmDialogViewModel>
    {
        public ElevateConfirmDialogViewModel ViewModel
        {
            get => (ElevateConfirmDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public ElevateConfirmDialog()
        {
            this.InitializeComponent();
        }

        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
    }
}