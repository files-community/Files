// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests.Dialogs
{
	public sealed partial class PropertiesDialog : WindowedDialog
	{
		public PropertiesDialog()
		{
			InitializeComponent();

			OverlappedPresenter presenter = OverlappedPresenter.Create();
			ExtendsContentIntoTitleBar = true;
			AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 720));
			presenter.IsMaximizable = false;
			AppWindow.SetPresenter(presenter);
			AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
			presenter.PreferredMinimumHeight = 480;
			presenter.PreferredMinimumWidth = 360;
		}

		private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			switch (sender.SelectedItem.Tag)
			{
				case nameof(PropertiesGeneralPage):
					MainContentFrame?.Navigate(typeof(PropertiesGeneralPage));
					break;
				case nameof(PropertiesDetailsPage):
					MainContentFrame?.Navigate(typeof(PropertiesDetailsPage));
					break;
			}
		}
	}
}
