using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>
	{
		public CreateShortcutDialogViewModel ViewModel
		{
			get => (CreateShortcutDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CreateShortcutDialog()
		{
			InitializeComponent();
		}	

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void DestinationItemPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(DestinationItemPath.Text))
			{
				ViewModel.IsLocationValid = false;
				return;
			}

			try
			{
				ViewModel.DestinationPathExists = Path.Exists(DestinationItemPath.Text) && DestinationItemPath.Text != Path.GetPathRoot(DestinationItemPath.Text);
				if (ViewModel.DestinationPathExists)
				{
					ViewModel.IsLocationValid = true;
				}
				else
				{
					var uri = new Uri(DestinationItemPath.Text);
					ViewModel.IsLocationValid = uri.IsWellFormedOriginalString();
				}
			}
			catch (Exception)
			{
				ViewModel.IsLocationValid = false;
			}
			ViewModel.DestinationItemPath = DestinationItemPath.Text;
		}
	}
}