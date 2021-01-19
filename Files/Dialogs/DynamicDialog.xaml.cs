using Files.ViewModels.Dialogs;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class DynamicDialog : ContentDialog
    {
        public DynamicDialogViewModel ViewModel { get; private set; }

        public DynamicDialog(DynamicDialogViewModel choiceDialogViewModel)
        {
            this.InitializeComponent();

            this.ViewModel = choiceDialogViewModel;
            this.DataContext = ViewModel;
        }
    }
}
