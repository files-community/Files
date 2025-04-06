// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class ThemedIconLayer
	{
		[GeneratedDependencyProperty(DefaultValue = ThemedIconLayerType.Base)]
		public partial ThemedIconLayerType LayerType { get; set; }

		[GeneratedDependencyProperty(DefaultValue = "")]
		public partial string PathData { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 16.0d)]
		public partial double LayerSize { get; set; }

		[GeneratedDependencyProperty]
		public partial Brush LayerColor { get; set; }

		[GeneratedDependencyProperty(DefaultValue = ThemedIconColorType.Normal)]
		public partial ThemedIconColorType IconColorType { get; set; }

		partial void OnLayerTypeChanged(ThemedIconLayerType newValue)
		{
			LayerTypeChanged(newValue);
		}

		partial void OnPathDataChanged(string newValue)
		{
			LayerPathDataChanged(newValue);
		}

		partial void OnLayerSizeChanged(double newValue)
		{
			LayerSizePropertyChanged(newValue);
		}

		partial void OnLayerColorChanged(Brush newValue)
		{
			IconColorTypeChanged();
		}

		partial void OnIconColorTypeChanged(ThemedIconColorType newValue)
		{
			IconColorTypeChanged();
		}
	}
}
