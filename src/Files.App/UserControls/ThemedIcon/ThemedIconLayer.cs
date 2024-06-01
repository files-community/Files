// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents a Path layer for the ThemedIcon control
	/// </summary>
	public sealed partial class ThemedIconLayer : Control
	{
		public ThemedIconLayer()
		{
			DefaultStyleKey = typeof(ThemedIconLayer);
		}

		protected override void OnApplyTemplate()
		{
			// Initialize with default values
			UpdateIconLayerState();

			base.OnApplyTemplate();
		}

		private void UpdateIconLayerState()
		{
			if (LayerType == ThemedIconLayerType.Accent)
			{
				VisualStateManager.GoToState(
				this,
				IconState switch
				{
					ThemedIconColorType.Critical => CriticalStateName,
					ThemedIconColorType.Caution => CautionStateName,
					ThemedIconColorType.Success => SuccessStateName,
					ThemedIconColorType.Neutral => NeutralStateName,
					ThemedIconColorType.Toggle => AccentStateName,
					_ => AccentStateName,
				},
				true);
			}
			else if (LayerType == ThemedIconLayerType.AccentContrast)
			{
				VisualStateManager.GoToState(
				this,
				IconState switch
				{
					ThemedIconColorType.Critical => CriticalBGStateName,
					ThemedIconColorType.Caution => CautionBGStateName,
					ThemedIconColorType.Success => SuccessBGStateName,
					ThemedIconColorType.Neutral => NeutralBGStateName,
					ThemedIconColorType.Toggle => AccentContrastStateName,
					_ => AccentContrastStateName,
				},
				true);
			}
			else
			{
				VisualStateManager.GoToState(
					this,
					LayerType switch
					{
						ThemedIconLayerType.Alt => AltStateName,
						_ => BaseStateName,
					},
					true);
			}
			

			SetPathData(PathData ?? string.Empty);

		}

		private void OnLayerTypeChanged()
		{
			UpdateIconLayerState(); 
			
		}

		private void OnIconStateChanged()
		{
			UpdateIconLayerState();
		}

		private void SetPathData(string pathData)
		{
			if (string.IsNullOrEmpty(pathData))
				return;

			var geometry = (Geometry)XamlReader.Load(
				$"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pathData}</Geometry>");

			if (GetTemplateChild(LayerPathPart) is Path path)
			{
				path.Data = geometry;
			}
		}
	}
}
