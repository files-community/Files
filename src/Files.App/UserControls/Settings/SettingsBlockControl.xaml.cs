using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Windows.Input;

namespace Files.App.UserControls.Settings
{
	[ContentProperty(Name = nameof(SettingsActionableElement))]
	public sealed partial class SettingsBlockControl : UserControl
	{
		public event EventHandler<bool>? Click;

		public static readonly DependencyProperty TitleProperty = DependencyProperty
			.Register(nameof(Title), typeof(string), typeof(SettingsBlockControl), new(null));

		public static readonly DependencyProperty DescriptionProperty = DependencyProperty
			.Register(nameof(Description), typeof(string), typeof(SettingsBlockControl), new(null));

		public static readonly DependencyProperty AdditionalDescriptionContentProperty = DependencyProperty
			.Register(nameof(AdditionalDescriptionContent), typeof(FrameworkElement), typeof(SettingsBlockControl), new(null));

		public static readonly DependencyProperty IconProperty = DependencyProperty
			.Register(nameof(Icon), typeof(IconElement), typeof(SettingsBlockControl), new(null));

		public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty
			.Register(nameof(ButtonCommand), typeof(ICommand), typeof(SettingsBlockControl), new(null));

		public static readonly DependencyProperty IsClickableProperty = DependencyProperty
			.Register(nameof(IsClickable), typeof(bool), typeof(SettingsBlockControl), new(false));

		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty
			.Register(nameof(IsExpanded), typeof(bool), typeof(SettingsBlockControl), new(false));

		public static readonly DependencyProperty ExpandableContentProperty = DependencyProperty
			.Register(nameof(ExpandableContent), typeof(FrameworkElement), typeof(SettingsBlockControl), new(null));

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

		public ICommand ButtonCommand
		{
			get => (ICommand)GetValue(ButtonCommandProperty);
			set => SetValue(ButtonCommandProperty, value);
		}

		public bool IsClickable
		{
			get => (bool)GetValue(IsClickableProperty);
			set => SetValue(IsClickableProperty, value);
		}

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public FrameworkElement ExpandableContent
		{
			get => (FrameworkElement)GetValue(ExpandableContentProperty);
			set => SetValue(ExpandableContentProperty, value);
		}

		public FrameworkElement? SettingsActionableElement { get; set; }

		public SettingsBlockControl() => InitializeComponent();

		private void SettingsBlockControl_Loaded(object _, RoutedEventArgs e)
		{
			Loaded -= SettingsBlockControl_Loaded;
			if (ActionableButton is not null)
				AutomationProperties.SetName(ActionableButton, Title);
		}

		private void Expander_Collapsed(Expander _, ExpanderCollapsedEventArgs e) => Click?.Invoke(this, false);
		private void Expander_Expanding(Expander _, ExpanderExpandingEventArgs e) => Click?.Invoke(this, true);
	}
}