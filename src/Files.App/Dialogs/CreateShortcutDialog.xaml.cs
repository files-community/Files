using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
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
			this.Closing += CreateShortcutDialog_Closing;

			InvalidPathWarning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = DestinationItemPath
			});
		}

		private void CreateShortcutDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			this.Closing -= CreateShortcutDialog_Closing;
			InvalidPathWarning.IsOpen = false;
		}

		public new Task<DialogResult> ShowAsync() => base.ShowAsync().AsTask().ContinueWith(t => (DialogResult)t.Result);
	}
}