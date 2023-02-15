using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class ColoredIcon : UserControl
	{
		public string BaseBackdropGlyph
		{
			get => (string)GetValue(BaseBackdropPathProperty);
			set => SetValue(BaseBackdropPathProperty, value);
		}

		public string BaseLayerGlyph
		{
			get => (string)GetValue(BaseLayerPathProperty);
			set => SetValue(BaseLayerPathProperty, value);
		}

		public string OverlayLayerGlyph
		{
			get => (string)GetValue(OverlayLayerPathProperty);
			set => SetValue(OverlayLayerPathProperty, value);
		}

		// Using a DependencyProperty as the backing store for BaseBackdropPath.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BaseBackdropPathProperty =
			DependencyProperty.Register(nameof(BaseBackdropGlyph), typeof(string), typeof(ColoredIcon), new PropertyMetadata(null));
		
		// Using a DependencyProperty as the backing store for OverlayLayerPath.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty OverlayLayerPathProperty =
			DependencyProperty.Register(nameof(OverlayLayerGlyph), typeof(string), typeof(ColoredIcon), new PropertyMetadata(null));

		// Using a DependencyProperty as the backing store for BaseLayerPath.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BaseLayerPathProperty =
			DependencyProperty.Register(nameof(BaseLayerGlyph), typeof(string), typeof(ColoredIcon), new PropertyMetadata(null));

		public ColoredIcon()
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
			else if (v == Resources["AppBarToggleButtonForegroundChecked"] || v == Resources["AppBarToggleButtonForegroundCheckedPressed"])
			{
				VisualStateManager.GoToState(this, "Checked", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}

		private void UserControl_Loading(FrameworkElement sender, object args)
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