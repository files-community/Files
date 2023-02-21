using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class OpacityIcon : Control
	{
		public OpacityIcon()
		{
			InitializeComponent();
		}

		private void ForegroundChanged(DependencyObject sender, DependencyProperty dp)
		{
			var v = sender.GetValue(dp);
			if (v == Resources["AppBarButtonForegroundDisabled"])
			{
				VisualStateManager.GoToState(this, "Disabled", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}

		private void OpacityIcon_Loading(FrameworkElement sender, object args)
		{
			// register a property change callback for the parent content presenter's foreground to allow reacting to button state changes, eg disabled
			var p = this.FindAscendant<ContentPresenter>();
			if (p is not null)
			{
				p.RegisterPropertyChangedCallback(ContentPresenter.ForegroundProperty, ForegroundChanged);
			}
		}
	}
}