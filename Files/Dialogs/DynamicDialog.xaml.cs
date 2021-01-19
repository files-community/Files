using Files.ViewModels.Dialogs;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class DynamicDialog : ContentDialog
    {
        public ChoiceDialogViewModel ChoiceDialogViewModel { get; private set; }

        public DynamicDialog(ChoiceDialogViewModel choiceDialogViewModel)
        {
            this.InitializeComponent();

            this.ChoiceDialogViewModel = choiceDialogViewModel;
            this.DataContext = ChoiceDialogViewModel;
        }
    }
}
