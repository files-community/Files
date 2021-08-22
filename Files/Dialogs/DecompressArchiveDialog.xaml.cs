using Files.ViewModels.Dialogs;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class DecompressArchiveDialog : ContentDialog
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
    }
}