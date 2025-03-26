// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class ThemedIcon : Control
	{
		[GeneratedDependencyProperty]
		public partial string FilledIconData { get; set; }

		[GeneratedDependencyProperty]
		public partial string OutlineIconData { get; set; }

		[GeneratedDependencyProperty]
		public partial Brush Color { get; set; }

		[GeneratedDependencyProperty(DefaultValue = ThemedIconTypes.Layered)]
		public partial ThemedIconTypes IconType { get; set; }

		[GeneratedDependencyProperty(DefaultValue = ThemedIconColorType.None)]
		public partial ThemedIconColorType IconColorType { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 16.0d)]
		public partial double IconSize { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsToggled { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsFilled { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsHighContrast { get; set; }

		[GeneratedDependencyProperty]
		public partial object Layers { get; set; }

		[GeneratedDependencyProperty(DefaultValue = ToggleBehaviors.Auto)]
		public partial ToggleBehaviors ToggleBehavior { get; set; }

		partial void OnFilledIconDataChanged(string newValue)
		{
			OnFilledIconChanged();
		}

		partial void OnOutlineIconDataChanged(string newValue)
		{
			OnOutlineIconChanged();
		}

		partial void OnColorChanged(Brush newValue)
		{
			OnIconTypeChanged();
			OnIconColorChanged();
		}

		partial void OnIconTypeChanged(ThemedIconTypes newValue)
		{
			OnIconTypeChanged();
		}

		partial void OnIconColorTypeChanged(ThemedIconColorType newValue)
		{
			OnIconColorTypeChanged();
		}

		partial void OnIconSizeChanged(double newValue)
		{
			UpdateVisualStates();
			OnIconSizeChanged();
		}

		partial void OnIsToggledChanged(bool newValue)
		{
			UpdateVisualStates();
		}

		partial void OnIsFilledChanged(bool newValue)
		{
			UpdateVisualStates();
		}

		partial void OnIsHighContrastChanged(bool newValue)
		{
			UpdateVisualStates();
		}

		partial void OnLayersChanged(object newValue)
		{
			UpdateVisualStates();
		}

		partial void OnToggleBehaviorChanged(ToggleBehaviors newValue)
		{
			UpdateVisualStates();
		}
	}
}
