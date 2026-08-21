// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;

namespace Files.App.UITests.Dialogs
{
	public sealed partial class PropertiesDialog : Window
	{
		public PropertiesDialog()
		{
			InitializeComponent();

			OverlappedPresenter presenter = OverlappedPresenter.Create();
			ExtendsContentIntoTitleBar = true;
			AppWindow.Resize(new Windows.Graphics.SizeInt32(640, 480));
			presenter.IsMaximizable = false;
			AppWindow.SetPresenter(presenter);
			AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
			presenter.PreferredMinimumHeight = 800;
			presenter.PreferredMinimumWidth = 800;

			MainContentFrame?.Navigate(typeof(PropertiesGeneralPage));
		}
	}
}
