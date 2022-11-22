using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Files.App.UserControls.Settings
{
	[ContentProperty(Name = nameof(SettingsActionableElement))]
	public sealed partial class SettingsDisplayControl : UserControl
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty
			.Register(nameof(Title), typeof(string), typeof(SettingsDisplayControl), new(null));

		public static readonly DependencyProperty DescriptionProperty = DependencyProperty
			.Register(nameof(Description), typeof(string), typeof(SettingsDisplayControl), new(null));

		public static readonly DependencyProperty AdditionalDescriptionContentProperty = DependencyProperty
			.Register(nameof(AdditionalDescriptionContent), typeof(FrameworkElement), typeof(SettingsDisplayControl), new(null));

		public static readonly DependencyProperty IconProperty = DependencyProperty
			.Register(nameof(Icon), typeof(IconElement), typeof(SettingsDisplayControl), new(null));

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public FrameworkElement AdditionalDescriptionContent
		{
			get => (FrameworkElement)GetValue(AdditionalDescriptionContentProperty);
			set => SetValue(AdditionalDescriptionContentProperty, value);
		}

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public FrameworkElement? SettingsActionableElement { get; set; }

		public SettingsDisplayControl()
		{
			InitializeComponent();
			VisualStateManager.GoToState(this, "NormalState", false);
		}

		private void MainPanel_SizeChanged(object _, SizeChangedEventArgs e)
		{
			if (ActionableElement is null || e.NewSize.Width == e.PreviousSize.Width)
				return;

			var stateToGoName = (ActionableElement.ActualWidth > e.NewSize.Width / 3) ? "CompactState" : "NormalState";
			VisualStateManager.GoToState(this, stateToGoName, false);
		}
	}
}