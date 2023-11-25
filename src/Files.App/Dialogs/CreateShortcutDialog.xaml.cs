// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>
	{
		public IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		public CreateShortcutDialogViewModel ViewModel { get; set; }

		private ElementTheme ThemeMode
			=> (ElementTheme)AppThemeModeService.ThemeMode;

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

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
