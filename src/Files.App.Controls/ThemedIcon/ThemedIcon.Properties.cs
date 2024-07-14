// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Controls
{
    public partial class ThemedIcon : Control
    {
        //
        //	Dependency Property Registration
        //
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
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Layers"/> property.
        /// </summary>
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register(
                nameof(Layers),
                typeof(object),
                typeof(ThemedIcon),
                new PropertyMetadata(null, (d, e) => ((ThemedIcon)d).OnLayersPropertyChanged((object)e.OldValue, (object)e.NewValue)));

        //
        //	Public Properties
        //
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

        /// <summary>
        /// Gets or sets a value indicating whether the Icon should use Toggled states.
        /// </summary>        
        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        /// <summary>
        /// Gets or sets the objects we use as Layers for the Layered Icon.
        /// </summary>
        public object Layers
        {
            get => (object)GetValue(LayersProperty);
            set => SetValue(LayersProperty, value);
        }

        //
        //	Property Change Events
        //
        protected virtual void OnFilledIconPropertyChanged(string oldValue, string newValue)
        {
            FilledIconPathUpdate();
        }

        protected virtual void OnOutlineIconPropertyChanged(string oldValue, string newValue)
        {
            OutlineIconPathUpdate();
        }

        protected virtual void OnLayersPropertyChanged(object oldValue, object newValue)
        {
            LayeredIconContentUpdate();
        }

        protected virtual void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
        {
            UpdateIconTypeStates();
        }

        protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
        {
            UpdateIconColorTypeStates();
        }

        protected virtual void OnIsToggledPropertyChanged(bool oldValue, bool newValue)
        {
            ToggleChanged(newValue);
        }
    }
}