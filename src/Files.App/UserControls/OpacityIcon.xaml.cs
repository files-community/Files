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


		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(OpacityIcon), new PropertyMetadata(null));

		private void IsEnabledChange(DependencyObject sender, DependencyProperty dp)
		{
			string state = sender.GetValue(dp) is false ? "Disabled" : "Normal";

			if (state == "Normal" && IsSelected)
				VisualStateManager.GoToState(this, "Selected", true);
			else
				VisualStateManager.GoToState(this, state, true);
		}

		private void OpacityIcon_Loading(FrameworkElement sender, object e)
		{
			var control = this.FindAscendant<Control>();
			control?.RegisterPropertyChangedCallback(IsEnabledProperty, IsEnabledChange);
		}

		private void OpacityIcon_Loaded(object sender, RoutedEventArgs e)
		{
			var control = this.FindAscendant<Control>();
			if (control is not null && !control.IsEnabled)
				VisualStateManager.GoToState(this, "Disabled", false);

			if (control is not null && control.IsEnabled && IsSelected)
				VisualStateManager.GoToState(this, "Selected", true);
		}
	}
}
