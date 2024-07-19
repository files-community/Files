// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
    public partial class ThemedIcon : Control
    {
        #region DEPENDENCY PROPERTY REGISTRATION

        // Path data string properties

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
        /// The backing <see cref="DependencyProperty"/> for the <see cref="OutlineIconData"/> property.
        /// </summary>
        public static readonly DependencyProperty OutlineIconDataProperty =
            DependencyProperty.Register(
                nameof(OutlineIconData),
                typeof(string),
                typeof(ThemedIcon),
                new PropertyMetadata(string.Empty, (d, e) => ((ThemedIcon)d).OnOutlineIconPropertyChanged((string)e.OldValue, (string)e.NewValue)));

        // Color properties

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Color"/> property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Brush),
                typeof(ThemedIcon),
                new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnColorPropertyChanged((Brush)e.OldValue, (Brush)e.NewValue)));

        // Enum properties

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
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IconColorType"/> property.
        /// </summary>
        public static readonly DependencyProperty IconColorTypeProperty =
            DependencyProperty.Register(
                nameof(IconColorType),
                typeof(ThemedIconColorType),
                typeof(ThemedIcon),
                new PropertyMetadata(ThemedIconColorType.None, (d, e) => ((ThemedIcon)d).OnIconColorTypePropertyChanged((ThemedIconColorType)e.OldValue, (ThemedIconColorType)e.NewValue)));

        // Double properties

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(ThemedIcon),
                new PropertyMetadata((double)16, (d, e) => ((ThemedIcon)d).OnIconSizePropertyChanged((double)e.OldValue, (double)e.NewValue)));

        // Boolean properties

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IsToggled"/> property.
        /// </summary>
        public static readonly DependencyProperty IsToggledProperty =
            DependencyProperty.Register(
                nameof(IsToggled),
                typeof(bool),
                typeof(ThemedIcon),
                new PropertyMetadata(defaultValue: false, (d, e) => ((ThemedIcon)d).OnIsToggledPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

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
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IsHighContrast"/> property.
        /// </summary>
        public static readonly DependencyProperty IsHighContrastProperty =
            DependencyProperty.Register(
                nameof(IsHighContrast),
                typeof(bool),
                typeof(ThemedIcon),
                new PropertyMetadata(defaultValue: false, (d, e) => ((ThemedIcon)d).OnIsHighContrastPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

        // Layers

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Layers"/> property.
        /// </summary>
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register(
                nameof(Layers),
                typeof(object),
                typeof(ThemedIcon),
                new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnLayersPropertyChanged((object)e.OldValue, (object)e.NewValue)));

        #endregion

        #region PUBLIC PROPERTIES

        // Public path data string properties

        /// <summary>
        /// Gets or sets the Filled Icon Path data as a String
        /// </summary>
        public string FilledIconData
        {
            get => (string)GetValue(FilledIconDataProperty);
            set => SetValue(FilledIconDataProperty, value);
        }

        /// <summary>
        /// Gets or sets the Outline Icon Path data as a String
        /// </summary>
        public string OutlineIconData
        {
            get => (string)GetValue(OutlineIconDataProperty);
            set => SetValue(OutlineIconDataProperty, value);
        }

        // Public color properties

        /// <summary>
        /// Gets or sets the Brush used for the Custom IconColorType
        /// </summary>
        public Brush Color
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        // Public enum properties

        /// <summary>
        /// Gets or sets an Enum value to choose from our three icon types, Outline, Filled, Layered
        /// </summary>
        public ThemedIconTypes IconType
        {
            get => (ThemedIconTypes)GetValue(IconTypeProperty);
            set => SetValue(IconTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets Enum values to choose from our icon states, Normal, Critical, Caution, Success, Neutral, Disabled
        /// </summary>
        public ThemedIconColorType IconColorType
        {
            get => (ThemedIconColorType)GetValue(IconColorTypeProperty);
            set => SetValue(IconColorTypeProperty, value);
        }

        // Public double properties

        // <summary>
        /// Gets or sets a value indicating the Icon's design size.
        /// </summary>        
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        // Public boolean properties

        /// <summary>
        /// Gets or sets a value indicating whether the Icon should use Toggled states.
        /// </summary>        
        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Icon should use Filled states.
        /// </summary>        
        public bool IsFilled
        {
            get => (bool)GetValue(IsFilledProperty);
            set => SetValue(IsFilledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Icon is in HighContrast state.
        /// </summary>        
        public bool IsHighContrast
        {
            get => (bool)GetValue(IsHighContrastProperty);
            set => SetValue(IsHighContrastProperty, value);
        }

        // Public object properties

        /// <summary>
        /// Gets or sets the objects we use as Layers for the Layered Icon.
        /// </summary>
        public object Layers
        {
            get => (object)GetValue(LayersProperty);
            set => SetValue(LayersProperty, value);
        }

        #endregion

        #region PROPERTY CHANGE EVENTS

        // Path data string changed events

        protected virtual void OnFilledIconPropertyChanged(string oldValue, string newValue)
        {
            UpdateFilledIconPath();
        }

        protected virtual void OnOutlineIconPropertyChanged(string oldValue, string newValue)
        {
            UpdateOutlineIconPath();
        }

        // Color changed events
        protected virtual void OnColorPropertyChanged(Brush oldValue, Brush newValue)
        {
            UpdateIconTypeStates();
        }		

        // Enum changed events

        protected virtual void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
        {
            UpdateIconTypeStates();
        }

        protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
        {
            UpdateIconColorTypeStates();
        }

        // Double changed events
        
        protected virtual void OnIconSizePropertyChanged(double oldValue, double newValue)
        {
            IconSizePropertyChanged(newValue);
        }

        // Boolean changed events

        protected virtual void OnIsToggledPropertyChanged(bool oldValue, bool newValue)
        {
            ToggleChanged(newValue);
        }

        protected virtual void OnIsFilledPropertyChanged(bool oldValue, bool newValue)
        {
            FilledChanged(newValue);
        }

        protected virtual void OnIsHighContrastPropertyChanged(bool oldValue, bool newValue)
        {
            HighContrastChanged(newValue);
        }

        // Object changed events

        protected virtual void OnLayersPropertyChanged(object oldValue, object newValue)
        {
            UpdateLayeredIconContent();
        }

        #endregion
    }
}
