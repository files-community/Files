// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class DevToolsPage : Page
	{
		public DevToolsPage()
		{
			InitializeComponent();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(DevToolsViewModel.IsEditingIDEConfig))
				SetVisualState(ActualWidth);
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Width == e.PreviousSize.Width)
				return;

			SetVisualState(e.NewSize.Width);
		}

		private void SetVisualState(double width)
		{
			var defaultPathWidth = 300;
			var pickIDEWidth = PickIDEExe.ActualWidth == 0 ? 64 : PickIDEExe.ActualWidth;
			var minWidth = defaultPathWidth + pickIDEWidth;
			var state = minWidth > width / 1.6 ? "CompactState" : "DefaultState";
			VisualStateManager.GoToState(this, state, false);
		}
	}
}
