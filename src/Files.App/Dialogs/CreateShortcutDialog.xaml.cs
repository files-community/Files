// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

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

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private void CreateShortcutDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			this.Closing -= CreateShortcutDialog_Closing;
			InvalidPathWarning.IsOpen = false;
		}
	}
}
