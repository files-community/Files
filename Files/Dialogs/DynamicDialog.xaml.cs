using Files.ViewModels.Dialogs;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class DynamicDialog : ContentDialog
    {
        public DynamicDialogViewModel ViewModel 
        {
            get => (DynamicDialogViewModel)DataContext;
            private set => DataContext = value;
        }

        public DynamicDialog(DynamicDialogViewModel choiceDialogViewModel)
        {
            this.InitializeComponent();

            choiceDialogViewModel.HideDialog = this.Hide;
            this.ViewModel = choiceDialogViewModel;
        }
    }
}
