// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Windows.Input;

namespace Files.App.UserControls.Settings
{
	[Obsolete("Do not use this class as Settings Control anymore, the control have been moved to SettingsCoard and SettingsExpander.")]
	[ContentProperty(Name = nameof(SettingsActionableElement))]
	public sealed partial class SettingsBlockControl : UserControl
	{
		public FrameworkElement? SettingsActionableElement { get; set; }

		#region Dependency Properties
		public static readonly DependencyProperty ButtonCommandProperty =
			DependencyProperty.Register(
				nameof(ButtonCommand),
				typeof(ICommand),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public ICommand ButtonCommand
		{
			get => (ICommand)GetValue(ButtonCommandProperty);
			set => SetValue(ButtonCommandProperty, value);
		}

		public static readonly DependencyProperty ExpandableContentProperty =
			DependencyProperty.Register(
				nameof(ExpandableContent),
				typeof(FrameworkElement),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public FrameworkElement ExpandableContent
		{
			get => (FrameworkElement)GetValue(ExpandableContentProperty);
			set => SetValue(ExpandableContentProperty, value);
		}

		public static readonly DependencyProperty AdditionalDescriptionContentProperty =
			DependencyProperty.Register(
				nameof(AdditionalDescriptionContent),
				typeof(FrameworkElement),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public FrameworkElement AdditionalDescriptionContent
		{
			get => (FrameworkElement)GetValue(AdditionalDescriptionContentProperty);
			set => SetValue(AdditionalDescriptionContentProperty, value);
		}

		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register(
				nameof(Title),
				typeof(string),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public static readonly DependencyProperty DescriptionProperty =
			DependencyProperty.Register(
				nameof(Description),
				typeof(string),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register(
				nameof(Icon),
				typeof(IconElement),
				typeof(SettingsBlockControl),
				new PropertyMetadata(null));

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static readonly DependencyProperty IsClickableProperty =
			DependencyProperty.Register(
				nameof(IsClickable),
				typeof(bool),
				typeof(SettingsBlockControl),
				new PropertyMetadata(false));

		public bool IsClickable
		{
			get => (bool)GetValue(IsClickableProperty);
			set => SetValue(IsClickableProperty, value);
		}

		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(
				nameof(IsExpanded),
				typeof(bool),
				typeof(SettingsBlockControl),
				new PropertyMetadata(false));

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}
		#endregion

		public event EventHandler<bool>? Click;

		public SettingsBlockControl()
		{
			InitializeComponent();
		}

		private void SettingsBlockControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ActionableButton is not null)
				AutomationProperties.SetName(ActionableButton, Title);
		}

		private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
		{
			Click?.Invoke(this, true);
		}

		private void Expander_Collapsed(Expander sender, ExpanderCollapsedEventArgs args)
		{
			Click?.Invoke(this, false);
		}
	}
}