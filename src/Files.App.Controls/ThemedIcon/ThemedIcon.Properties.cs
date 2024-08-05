// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class ThemedIcon
	{
		#region FilledIconData (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="FilledIconData"/> property.
		/// </summary>
		public static readonly DependencyProperty FilledIconDataProperty =
			DependencyProperty.Register(
				nameof(FilledIconData),
				typeof(string),
				typeof(ThemedIcon),
				new PropertyMetadata(string.Empty, (d, e) => ((ThemedIcon)d).OnFilledIconPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		/// <summary>
		/// Gets or sets the Filled Icon Path data as a String
		/// </summary>
		public string FilledIconData
		{
			get => (string)GetValue(FilledIconDataProperty);
			set => SetValue(FilledIconDataProperty, value);
		}

		protected virtual void OnFilledIconPropertyChanged(string oldValue, string newValue)
		{
			OnFilledIconChanged();
		}

		#endregion

		#region OutlineIconData (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="OutlineIconData"/> property.
		/// </summary>
		public static readonly DependencyProperty OutlineIconDataProperty =
			DependencyProperty.Register(
				nameof(OutlineIconData),
				typeof(string),
				typeof(ThemedIcon),
				new PropertyMetadata(string.Empty, (d, e) => ((ThemedIcon)d).OnOutlineIconPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		/// <summary>
		/// Gets or sets the Outline Icon Path data as a String
		/// </summary>
		public string OutlineIconData
		{
			get => (string)GetValue(OutlineIconDataProperty);
			set => SetValue(OutlineIconDataProperty, value);
		}

		protected virtual void OnOutlineIconPropertyChanged(string oldValue, string newValue)
		{
			OnOutlineIconChanged();
		}

		#endregion

		#region Color (Brush)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Color"/> property.
		/// </summary>
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				nameof(Color),
				typeof(Brush),
				typeof(ThemedIcon),
				new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnColorPropertyChanged((Brush)e.OldValue, (Brush)e.NewValue)));

		/// <summary>
		/// Gets or sets the Brush used for the Custom IconColorType
		/// </summary>
		public Brush Color
		{
			get => (Brush)GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}

		protected virtual void OnColorPropertyChanged(Brush oldValue, Brush newValue)
		{
			OnIconTypeChanged();
		}

		#endregion

		#region IconType (enum ThemedIconTypes)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IconType"/> property.
		/// </summary>
		public static readonly DependencyProperty IconTypeProperty =
			DependencyProperty.Register(
				nameof(IconType),
				typeof(ThemedIconTypes),
				typeof(ThemedIcon),
				new PropertyMetadata(ThemedIconTypes.Layered, (d, e) => ((ThemedIcon)d).OnIconTypePropertyChanged((ThemedIconTypes)e.OldValue, (ThemedIconTypes)e.NewValue)));

		/// <summary>
		/// Gets or sets an Enum value to choose from our three icon types, Outline, Filled, Layered
		/// </summary>
		public ThemedIconTypes IconType
		{
			get => (ThemedIconTypes)GetValue(IconTypeProperty);
			set => SetValue(IconTypeProperty, value);
		}

		protected virtual void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
		{
			OnIconTypeChanged();
		}

		#endregion

		#region IconColorType (enum ThemedIconColorType)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IconColorType"/> property.
		/// </summary>
		public static readonly DependencyProperty IconColorTypeProperty =
			DependencyProperty.Register(
				nameof(IconColorType),
				typeof(ThemedIconColorType),
				typeof(ThemedIcon),
				new PropertyMetadata(ThemedIconColorType.None, (d, e) => ((ThemedIcon)d).OnIconColorTypePropertyChanged((ThemedIconColorType)e.OldValue, (ThemedIconColorType)e.NewValue)));

		/// <summary>
		/// Gets or sets Enum values to choose from our icon states, Normal, Critical, Caution, Success, Neutral, Disabled
		/// </summary>
		public ThemedIconColorType IconColorType
		{
			get => (ThemedIconColorType)GetValue(IconColorTypeProperty);
			set => SetValue(IconColorTypeProperty, value);
		}

		protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			OnIconColorTypeChanged();
		}

		#endregion

		#region IconSize (double)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IconSize"/> property.
		/// </summary>
		public static readonly DependencyProperty IconSizeProperty =
			DependencyProperty.Register(
				nameof(IconSize),
				typeof(double),
				typeof(ThemedIcon),
				new PropertyMetadata((double)16, (d, e) => ((ThemedIcon)d).OnIconSizePropertyChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets a value indicating the Icon's design size.
		/// </summary>
		public double IconSize
		{
			get => (double)GetValue(IconSizeProperty);
			set => SetValue(IconSizeProperty, value);
		}

		protected virtual void OnIconSizePropertyChanged(double oldValue, double newValue)
		{
			UpdateVisualStates();
		}

		#endregion

		#region ToggleBehavior (enum ToggleBehaviors)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ToggleBehavior"/> property.
		/// </summary>
		public static readonly DependencyProperty ToggleBehaviorProperty =
			DependencyProperty.Register(
				nameof(ToggleBehavior),
				typeof(ToggleBehaviors),
				typeof(ThemedIcon),
				new PropertyMetadata(defaultValue: ToggleBehaviors.Auto, (d, e) => ((ThemedIcon)d).OnToggleBehaviorPropertyChanged((ToggleBehaviors)e.OldValue, (ToggleBehaviors)e.NewValue)));

		/// <summary>
		/// Gets or sets a value indicating whether the Icon should use Toggled states.
		/// </summary>
		public ToggleBehaviors ToggleBehavior
		{
			get => (ToggleBehaviors)GetValue(ToggleBehaviorProperty);
			set => SetValue(ToggleBehaviorProperty, value);
		}

		protected virtual void OnToggleBehaviorPropertyChanged(ToggleBehaviors oldValue, ToggleBehaviors newValue)
		{
			UpdateVisualStates();
		}

		#endregion

		#region IsFilled (bool)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IsFilled"/> property.
		/// </summary>
		public static readonly DependencyProperty IsFilledProperty =
			DependencyProperty.Register(
				nameof(IsFilled),
				typeof(bool),
				typeof(ThemedIcon),
				new PropertyMetadata(defaultValue: false, (d, e) => ((ThemedIcon)d).OnIsFilledPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

		/// <summary>
		/// Gets or sets a value indicating whether the Icon should use Filled states.
		/// </summary>
		public bool IsFilled
		{
			get => (bool)GetValue(IsFilledProperty);
			set => SetValue(IsFilledProperty, value);
		}

		protected virtual void OnIsFilledPropertyChanged(bool oldValue, bool newValue)
		{
			UpdateVisualStates();
		}

		#endregion

		#region IsHighContrast (bool)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IsHighContrast"/> property.
		/// </summary>
		public static readonly DependencyProperty IsHighContrastProperty =
			DependencyProperty.Register(
				nameof(IsHighContrast),
				typeof(bool),
				typeof(ThemedIcon),
				new PropertyMetadata(defaultValue: false, (d, e) => ((ThemedIcon)d).OnIsHighContrastPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

		/// <summary>
		/// Gets or sets a value indicating whether the Icon is in HighContrast state.
		/// </summary>
		public bool IsHighContrast
		{
			get => (bool)GetValue(IsHighContrastProperty);
			set => SetValue(IsHighContrastProperty, value);
		}

		protected virtual void OnIsHighContrastPropertyChanged(bool oldValue, bool newValue)
		{
			UpdateVisualStates();
		}

		#endregion

		#region Layers (object)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Layers"/> property.
		/// </summary>
		public static readonly DependencyProperty LayersProperty =
			DependencyProperty.Register(
				nameof(Layers),
				typeof(object),
				typeof(ThemedIcon),
				new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnLayersPropertyChanged((object)e.OldValue, (object)e.NewValue)));

		/// <summary>
		/// Gets or sets the objects we use as Layers for the Layered Icon.
		/// </summary>
		public object Layers
		{
			get => (object)GetValue(LayersProperty);
			set => SetValue(LayersProperty, value);
		}

		protected virtual void OnLayersPropertyChanged(object oldValue, object newValue)
		{
			OnLayeredIconChanged();
		}

		#endregion
	}
}
