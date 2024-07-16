// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
    public partial class ThemedIconLayer
    {
        #region DEPENDENCY PROPERTY REGISTRATION
        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="LayerType"/> property.
        /// </summary>
        public static readonly DependencyProperty LayerTypeProperty =
            DependencyProperty.Register(
                nameof(LayerType),
                typeof(ThemedIconLayerType),
                typeof(ThemedIconLayer),
                new PropertyMetadata(ThemedIconLayerType.Base, (d, e) => ((ThemedIconLayer)d).OnLayerTypePropertyChanged((ThemedIconLayerType)e.OldValue, (ThemedIconLayerType)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="PathData"/> property.
        /// </summary>
        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register(
                nameof(PathData),
                typeof(string),
                typeof(ThemedIconLayer),
                new PropertyMetadata(string.Empty, (d, e) => ((ThemedIconLayer)d).OnLayerPathDataPropertyChanged((string)e.OldValue, (string)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IconColorType"/> property.
        /// </summary>
        public static readonly DependencyProperty IconColorTypeProperty =
            DependencyProperty.Register(
                nameof(IconColorType),
                typeof(ThemedIconColorType),
                typeof(ThemedIconLayer),
                new PropertyMetadata(ThemedIconColorType.Normal, (d, e) => ((ThemedIconLayer)d).OnIconColorTypePropertyChanged((ThemedIconColorType)e.OldValue, (ThemedIconColorType)e.NewValue)));
        #endregion

        #region PUBLIC PROPERTIES
        /// <summary>
        /// Gets or sets the Enum value for LayerType of the layer
        /// </summary>
        public ThemedIconLayerType LayerType
        {
            get => (ThemedIconLayerType)GetValue(LayerTypeProperty);
            set => SetValue(LayerTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the PathData value for the layer
        /// </summary>
        public string PathData
        {
            get => (string)GetValue(PathDataProperty);
            set => SetValue(PathDataProperty, value);
        }
        /// <summary>
        /// Gets or sets the Enum value for IconColorType of the layer
        /// </summary>
        public ThemedIconColorType IconColorType
        {
            get => (ThemedIconColorType)GetValue(IconColorTypeProperty);
            set => SetValue(IconColorTypeProperty, value);
        }
        #endregion

        #region PROPERTY CHANGE EVENTS
        protected virtual void OnLayerTypePropertyChanged(ThemedIconLayerType oldValue, ThemedIconLayerType newValue)
        {
            LayerTypeChanged(newValue);
        }

        protected virtual void OnLayerPathDataPropertyChanged(string oldValue, string newValue)
        {
            LayerPathDataChanged(newValue);
        }

        protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
        {
            IconColorTypeChanged(newValue);
        }
        #endregion
    }
}