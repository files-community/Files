// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : UserControl
	{
		private RecentFilesWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<RecentFilesWidgetViewModel>();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}
	}
}
