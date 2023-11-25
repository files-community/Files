// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class AddBranchDialog : ContentDialog, IDialog<AddBranchDialogViewModel>
	{
		public IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		public AddBranchDialogViewModel ViewModel { get; set; }

		private ElementTheme ThemeMode
			=> (ElementTheme)AppThemeModeService.ThemeMode;

		public AddBranchDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
		}
	}
}
