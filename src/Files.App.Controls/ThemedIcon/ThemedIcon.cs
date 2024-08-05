// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using System.Linq;
using System.Collections.Generic;

namespace Files.App.Controls
{
	/// <summary>
	/// A control for a State and Color aware Icon
	/// </summary>
	public partial class ThemedIcon : Control
	{
		public ThemedIcon()
		{
			DefaultStyleKey = typeof(ThemedIcon);
		}

		protected override void OnApplyTemplate()
		{
			IsEnabledChanged -= OnIsEnabledChanged;

			base.OnApplyTemplate();

			IsEnabledChanged += OnIsEnabledChanged;

			_isOwnerEnabled = IsEnabled;

			FindOwnerControlStates();
			OnFilledIconChanged();
			OnOutlineIconChanged();
			OnLayeredIconChanged();

			OnIconTypeChanged();
			OnIconColorTypeChanged();
		}

		// Updates paths and layers

		private void OnFilledIconChanged()
		{
			// Updates Filled Icon from Path Data
			if (GetTemplateChild(FilledPathIconViewBox) is not Viewbox filledViewBox)
				return;

			SetPathData(FilledIconPath, FilledIconData ?? string.Empty, filledViewBox);
		}

		private void OnOutlineIconChanged()
		{
			// Updates Outline Icon from Path Data
			if (GetTemplateChild(OutlinePathIconViewBox) is not Viewbox outlineViewBox)
				return;

			SetPathData(OutlineIconPath, OutlineIconData ?? string.Empty, outlineViewBox);
		}

		private void OnLayeredIconChanged()
		{
			// Updates Layered Icon from it's Layers
			if (GetTemplateChild(LayeredPathIconViewBox) is not Viewbox layeredViewBox ||
				GetTemplateChild(LayeredPathCanvas) is not Canvas canvas ||
				Layers is not ICollection<ThemedIconLayer> layers)
				return;

			canvas.Children.Clear();

			foreach (var layer in layers)
			{
				canvas.Children.Add(
					new ThemedIconLayer()
					{
						LayerType = layer.LayerType,
						IconColorType = layer.IconColorType,
						PathData = layer.PathData,
						Opacity = layer.Opacity,
						LayerColor = Color,
						Foreground = Foreground,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						LayerSize = IconSize,
						Width = layer.LayerSize,
						Height = layer.LayerSize

					});
			}
		}

		// Updates visual states

		private void OnIconTypeChanged()
		{
			switch (ToggleBehavior)
			{
				case ToggleBehaviors.Auto:
					{
						if (_isOwnerToggled is true || IsFilled is true)
						{
							VisualStateManager.GoToState(this, FilledTypeStateName, true);
							return;
						}
						else if (IsHighContrast is true || _isOwnerEnabled is false || IsEnabled is false)
						{
							VisualStateManager.GoToState(this, OutlineTypeStateName, true);
							VisualStateManager.GoToState(this, DisabledStateName, true);
							return;
						}
						else
						{
							VisualStateManager.GoToState(
								this,
								IconType is ThemedIconTypes.Layered ? LayeredTypeStateName : OutlineTypeStateName,
								true);
						}
					}
					break;
				case ToggleBehaviors.Off:
					{
						if (IsFilled is true)
						{
							VisualStateManager.GoToState(this, FilledTypeStateName, true);
							return;
						}
						else if (IsHighContrast is true || _isOwnerEnabled is false || IsEnabled is false)
						{
							VisualStateManager.GoToState(this, OutlineTypeStateName, true);
							VisualStateManager.GoToState(this, DisabledStateName, true);
							return;
						}
						else
						{
							VisualStateManager.GoToState(
								this,
								IconType is ThemedIconTypes.Layered ? LayeredTypeStateName : OutlineTypeStateName,
								true);
						}
					}
					break;
				case ToggleBehaviors.On:
					{
						VisualStateManager.GoToState(this, FilledTypeStateName, true);
					}
					break;
			}

			VisualStateManager.GoToState(this, EnabledStateName, true);
		}

		private void OnIconColorTypeChanged()
		{
			if (_isOwnerEnabled && IsEnabled)
			{
				if ((ToggleBehavior is ToggleBehaviors.Auto && _isOwnerToggled) ||
					ToggleBehavior is ToggleBehaviors.On)
				{
					// Toggle
					VisualStateManager.GoToState(this, ToggleStateName, true);
				}
				else
				{
					// Use colorful ones
					VisualStateManager.GoToState(
						this,
						IconColorType switch
						{
							ThemedIconColorType.Critical => CriticalStateName,
							ThemedIconColorType.Caution => CautionStateName,
							ThemedIconColorType.Success => SuccessStateName,
							ThemedIconColorType.Neutral => NeutralStateName,
							ThemedIconColorType.Accent => AccentStateName,
							ThemedIconColorType.Custom => CustomColorStateName,
							_ => NormalStateName,
						},
						true);
				}

				// Update layered icon color
				if (GetTemplateChild(LayeredPathCanvas) is Canvas canvas)
					foreach (var layer in canvas.Children.Cast<ThemedIconLayer>())
						layer.IconColorType = IconColorType;
			}
			else
			{
				// Disable + toggle
				if ((ToggleBehavior is ToggleBehaviors.Auto && _isOwnerToggled is true) ||
					ToggleBehavior is ToggleBehaviors.On)
					VisualStateManager.GoToState(this, DisabledToggleColorStateName, true);
				// Disable
				else
					VisualStateManager.GoToState(this, DisabledColorStateName, true);
			}
		}

		// Misc

		private void UpdateVisualStates()
		{
			OnIconTypeChanged();
			OnIconColorTypeChanged();
		}

		private void SetPathData(string partName, string pathData, FrameworkElement element)
		{
			// Updates PathData
			if (string.IsNullOrEmpty(pathData))
				return;

			var geometry = (Geometry)XamlReader.Load(
				$"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pathData}</Geometry>");

			if (GetTemplateChild(partName) is Path path)
			{
				path.Data = geometry;
				path.Width = IconSize;
				path.Height = IconSize;
			}
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateVisualStates();
		}
	}
}
