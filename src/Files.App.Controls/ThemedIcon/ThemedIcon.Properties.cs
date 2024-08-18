// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	[DependencyProperty<string>("FilledIconData", nameof(OnFilledIconPropertyChanged))]
	[DependencyProperty<string>("OutlineIconData", nameof(OnOutlineIconPropertyChanged))]
	[DependencyProperty<Brush>("Color", nameof(OnColorPropertyChanged))]
	[DependencyProperty<ThemedIconTypes>("IconType", nameof(OnIconTypePropertyChanged), DefaultValue = ThemedIconTypes.Layered)]
	[DependencyProperty<ThemedIconColorType>("IconColorType", nameof(OnIconColorTypePropertyChanged), DefaultValue = ThemedIconColorType.None)]
	[DependencyProperty<double>("IconSize", nameof(OnIconSizePropertyChanged), DefaultValue = (double)16)]
	[DependencyProperty<bool>("IsToggled", nameof(OnIsToggledPropertyChanged), DefaultValue = false)]
	[DependencyProperty<bool>("IsFilled", nameof(OnIsFilledPropertyChanged), DefaultValue = false)]
	[DependencyProperty<bool>("IsHighContrast", nameof(OnIsHighContrastPropertyChanged), DefaultValue = false)]
	[DependencyProperty<object>("Layers", nameof(OnLayersPropertyChanged))]
	public partial class ThemedIcon : Control
	{
		protected virtual void OnFilledIconPropertyChanged(string oldValue, string newValue)
		{
			UpdateFilledIconPath();
		}

		protected virtual void OnOutlineIconPropertyChanged(string oldValue, string newValue)
		{
			UpdateOutlineIconPath();
		}

		protected virtual void OnColorPropertyChanged(Brush oldValue, Brush newValue)
		{
			UpdateIconTypeStates();
		}

		protected virtual void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
		{
			UpdateIconTypeStates();
		}

		protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			UpdateIconColorTypeStates();
		}

		protected virtual void OnIconSizePropertyChanged(double oldValue, double newValue)
		{
			IconSizePropertyChanged(newValue);
		}

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

		protected virtual void OnLayersPropertyChanged(object oldValue, object newValue)
		{
			UpdateLayeredIconContent();
		}
	}
}
