// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class DevToolsPage : Page
	{
		public DevToolsPage()
		{
			InitializeComponent();
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Width == e.PreviousSize.Width)
				return;

			var defaultPathWidth = 300;
			var pickIDEWidth = PickIDEExe.ActualWidth == 0 ? 64 : PickIDEExe.ActualWidth;

			var minWidth = defaultPathWidth + pickIDEWidth;
			var state = minWidth > e.NewSize.Width / 1.6 ? "CompactState" : "DefaultState";
			VisualStateManager.GoToState(this, state, false);
		}
	}
}
