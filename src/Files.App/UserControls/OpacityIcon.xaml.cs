using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class OpacityIcon : Control
	{
		public OpacityIcon()
		{
			InitializeComponent();
		}

		private void IsEnabledChange(DependencyObject sender, DependencyProperty dp)
		{
			string state = sender.GetValue(dp) is false ? "Disabled" : "Normal";
			VisualStateManager.GoToState(this, state, true);
		}

		private void OpacityIcon_Loading(FrameworkElement sender, object e)
		{
			// register a property change callback for the parent content presenter's foreground to allow reacting to button state changes, eg disabled
			var control = this.FindAscendant<Control>();
			control?.RegisterPropertyChangedCallback(IsEnabledProperty, IsEnabledChange);
		}

		private void OpacityIcon_Loaded(object sender, RoutedEventArgs e)
		{
			var control = this.FindAscendant<Control>();
			if (control is not null && !control.IsEnabled)
				VisualStateManager.GoToState(this, "Disabled", true);
		}
	}
}
