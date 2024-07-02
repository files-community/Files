// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class ThemedIcon : Control
	{
		/// <summary>
		/// Gets or sets the Filled Icon Path data as a String
		/// </summary>
		public string FilledIconData
		{
			get => (string)GetValue(FilledIconDataProperty);
			set => SetValue(FilledIconDataProperty, value);
		}

		public static readonly DependencyProperty FilledIconDataProperty =
			DependencyProperty.Register(
				nameof(FilledIconData),
				typeof(string),
				typeof(ThemedIcon),
				new PropertyMetadata(string.Empty, (d, e) => ((ThemedIcon)d).OnFilledIconPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		/// <summary>
		/// Gets or sets the Outline Icon Path data as a String
		/// </summary>
		public string OutlineIconData
		{
			get => (string)GetValue(OutlineIconDataProperty);
			set => SetValue(OutlineIconDataProperty, value);
		}

		public static readonly DependencyProperty OutlineIconDataProperty =
			DependencyProperty.Register(
				nameof(OutlineIconData),
				typeof(string),
				typeof(ThemedIcon),
				new PropertyMetadata(string.Empty, (d, e) => ((ThemedIcon)d).OnOutlineIconPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		/// <summary>
		/// Enum to choose from our three icon types, Outline, Filled, Layered
		/// </summary>
		public ThemedIconTypes IconType
		{
			get => (ThemedIconTypes)GetValue(IconTypeProperty);
			set => SetValue(IconTypeProperty, value);
		}

		public static readonly DependencyProperty IconTypeProperty =
			DependencyProperty.Register(
				nameof(IconType),
				typeof(ThemedIconTypes),
				typeof(ThemedIcon),
				new PropertyMetadata(ThemedIconTypes.Layered, (d, e) => ((ThemedIcon)d).OnIconTypePropertyChanged((ThemedIconTypes)e.OldValue, (ThemedIconTypes)e.NewValue)));

		/// <summary>
		/// Enum to choose from our icon states, Normal, Critical, Caution, Success, Neutral, Disabled
		/// </summary>
		public ThemedIconColorType IconState
		{
			get => (ThemedIconColorType)GetValue(IconStateProperty);
			set => SetValue(IconStateProperty, value);
		}

		public static readonly DependencyProperty IconStateProperty =
			DependencyProperty.Register(
				nameof(IconState),
				typeof(ThemedIconColorType),
				typeof(ThemedIcon),
				new PropertyMetadata(ThemedIconColorType.None, (d, e) => ((ThemedIcon)d).OnIconStatePropertyChanged((ThemedIconColorType)e.OldValue, (ThemedIconColorType)e.NewValue)));

		/// <summary>
		/// Gets or sets a value indicating whether the Icon should adapt to High Contrast states.
		/// </summary>
		public bool UseContrast
		{
			get => (bool)GetValue(UseContrastProperty);
			set => SetValue(UseContrastProperty, value);
		}

		public static readonly DependencyProperty UseContrastProperty =
			DependencyProperty.Register(
				nameof(UseContrast),
				typeof(bool),
				typeof(ThemedIcon),
				new PropertyMetadata(false, null));

		/// <summary>
		/// Gets or sets a value indicating whether the Icon should use Toggle colors.
		/// </summary>
		public bool IsToggled
		{
			get => (bool)GetValue(IsToggledProperty);
			set => SetValue(IsToggledProperty, value);
		}

		public static readonly DependencyProperty IsToggledProperty =
			DependencyProperty.Register(
				nameof(IsToggled),
				typeof(bool),
				typeof(ThemedIcon),
				new PropertyMetadata(defaultValue: false, (d, e) => ((ThemedIcon)d).OnIsToggledPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

		/// <summary>
		/// Gets the objects we use as Layers for the Layered Icon.
		/// </summary>
		public object Layers
		{
			get => (object)GetValue(LayersProperty);
			set => SetValue(LayersProperty, value);
		}

		public static readonly DependencyProperty LayersProperty =
			DependencyProperty.Register(
				nameof(Layers),
				typeof(object),
				typeof(ThemedIcon),
				new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnLayersPropertyChanged((object)e.OldValue, (object)e.NewValue)));

		private void OnFilledIconPropertyChanged(string oldValue, string newValue)
		{
			OnFilledIconChanged();
		}

		private void OnOutlineIconPropertyChanged(string oldValue, string newValue)
		{
			OnOutlineIconChanged();
		}

		private void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
		{
			OnIconTypeChanged();
		}

		private void OnIconStatePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			OnIconStateChanged();
		}

		private void OnLayersPropertyChanged(object oldValue, object newValue)
		{
			OnLayeredIconContentChanged();
		}

		private void OnIsToggledPropertyChanged(bool oldValue, bool newValue)
		{
			OnToggleChanged(newValue);
		}
	}
}
