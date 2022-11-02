using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
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