// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.UserControls
{
	public sealed partial class ThemedIconLayer
	{
		public ThemedIconLayerType LayerType
		{
			get => (ThemedIconLayerType)GetValue(LayerTypeProperty);
			set => SetValue(LayerTypeProperty, value);
		}

		public static readonly DependencyProperty LayerTypeProperty =
			DependencyProperty.Register(
				nameof(LayerType),
				typeof(ThemedIconLayerType),
				typeof(ThemedIconLayer),
				new PropertyMetadata(ThemedIconLayerType.Base, (d, e) => ((ThemedIconLayer)d).OnLayerTypePropertyChanged((ThemedIconLayerType)e.OldValue, (ThemedIconLayerType)e.NewValue)));

		/// <summary>
		/// 
		/// </summary>
		public string PathData
		{
			get => (string)GetValue(PathDataProperty);
			set => SetValue(PathDataProperty, value);
		}

		public static readonly DependencyProperty PathDataProperty =
			DependencyProperty.Register(
				nameof(PathData),
				typeof(string),
				typeof(ThemedIconLayer),
				new PropertyMetadata(string.Empty));

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
				typeof(ThemedIconLayer),
				new PropertyMetadata(ThemedIconColorType.Normal, (d, e) => ((ThemedIconLayer)d).OnIconStatePropertyChanged((ThemedIconColorType)e.OldValue, (ThemedIconColorType)e.NewValue)));

		private void OnLayerTypePropertyChanged(ThemedIconLayerType oldValue, ThemedIconLayerType newValue)
		{
			OnLayerTypeChanged();
		}

		private void OnIconStatePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			OnIconStateChanged();
		}
	}
}
