// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents control for Files rich glyph icon.
	/// </summary>
	public sealed partial class ThemedIcon : Control
	{
		private IAppThemeModeService AppThemeModeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		private bool _isHighContrast = false;
		private bool _isToggled = false;

		ToggleButton? ownerToggleButton = null;

		private ThemedIconColorType _defaultColorType = ThemedIconColorType.None;

		public ThemedIcon()
		{
			DefaultStyleKey = typeof(ThemedIcon);
		}

		protected override void OnApplyTemplate()
		{
			ownerToggleButton = this.FindAscendant<ToggleButton>();

			if (ownerToggleButton != null)
			{
				ownerToggleButton.Checked += OwnerCheckedChanged;
				ownerToggleButton.Unchecked += OwnerCheckedChanged;
			}

			var control = this.FindAscendant<Control>();
			if (control != null)
			{
				control.IsEnabledChanged += (s, e) =>
				{
					OnIconStateChanged();
				};
			}

			OnToggleChanged(_isToggled);
			OnFilledIconChanged();
			OnOutlineIconChanged();
			OnLayeredIconContentChanged();
			OnIconTypeChanged();
			OnIconStateChanged();

			// Get notified when whether the theme is High Contrast or not is changed.
			AppThemeModeService.IsHighContrastChanged += (s, e) =>
			{
				_isHighContrast = e;

				OnIconTypeChanged();
				OnIconStateChanged();
			};

			base.OnApplyTemplate();
		}

		// Code to respond to owner checked changes
		private void OwnerCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (ownerToggleButton is null)
				return;

			OnToggleChanged(ownerToggleButton.IsChecked is true);
		}

		// Code for handling the IsToggled property change.
		private void OnToggleChanged(bool value)
		{	
			_isToggled = value;

			OnIconTypeChanged();
			OnIconStateChanged();
		}

		private void OnFilledIconChanged()
		{
			if (GetTemplateChild(FilledPathIconViewBox) is not Viewbox filledViewBox)
				return;

			SetPathData(FilledIconPath, FilledIconData ?? string.Empty, filledViewBox);
		}

		private void OnOutlineIconChanged()
		{
			if (GetTemplateChild(OutlinePathIconViewBox) is not Viewbox outlineViewBox)
				return;

			SetPathData(OutlineIconPath, OutlineIconData ?? string.Empty, outlineViewBox);
		}

		private void OnLayeredIconContentChanged()
		{
			if (GetTemplateChild(LayeredPathIconViewBox) is not Viewbox layeredViewBox ||
				GetTemplateChild(LayeredPathCanvas) is not Canvas canvas ||
				Layers is not ICollection<ThemedIconLayer> layers)
				return;

			foreach (var layer in layers)
			{
				canvas.Children.Add(
					new ThemedIconLayer()
					{
						LayerType = layer.LayerType,
						IconState = layer.IconState,
						PathData = layer.PathData,
						Opacity = layer.Opacity,
					});
			}
		}

		// Handles changes to the Icon Type and setting the correct Visual State.
		private void OnIconTypeChanged()
		{
			// If Toggled, only show Filled Icon state.
			if (_isToggled)
				VisualStateManager.GoToState(this, FilledTypeStateName, true);

			// If using Contrast mode, bypass Layered Icon state to go to Outline Visual State.
			else if (UseContrast || _isHighContrast)
			{
				VisualStateManager.GoToState(
					this,
					IconType switch
					{
						ThemedIconTypes.Outline => OutlineTypeStateName,
						ThemedIconTypes.Filled => FilledTypeStateName,
						_ => OutlineTypeStateName,
					},
					true);
			}
			// Else allow all three icon states to show.
			else
			{
				VisualStateManager.GoToState(
					this,
					IconType switch
					{
						ThemedIconTypes.Outline => OutlineTypeStateName,
						ThemedIconTypes.Filled => FilledTypeStateName,
						_ => LayeredTypeStateName,
					},
					true);
			}

			// If the Icon is disabled, switch from Layered to Outline Visual State.
			if (!IsEnabled)
			{
				if (IconType == ThemedIconTypes.Layered)
					VisualStateManager.GoToState(this, OutlineTypeStateName, true);

				VisualStateManager.GoToState(this, NotEnabledStateName, true);
			}
		}

		private void OnIconStateChanged()
		{
			// If the Icon IsToggled, we go to the Toggle Visual State
			if (_isToggled)
			{
				VisualStateManager.GoToState(this, ToggleStateName, true);
			}
			// Else if in Contrast mode, we go direct to the HighContrast Visual State.
			else if (UseContrast || _isHighContrast)
			{
				VisualStateManager.GoToState(this, HighContrastStateName, true);
			}
			else
			{
				VisualStateManager.GoToState(
					this,
					IconState switch
					{
						ThemedIconColorType.Critical => CriticalStateName,
						ThemedIconColorType.Caution => CautionStateName,
						ThemedIconColorType.Success => SuccessStateName,
						ThemedIconColorType.Neutral => NeutralStateName,
						_ => NormalStateName,
					},
					true);

				if (GetTemplateChild(LayeredPathCanvas) is Canvas canvas)
				{
					foreach (var layer in canvas.Children.Cast<ThemedIconLayer>())
					{
						layer.IconState = IconState;
					}
				}
			}

			if (IsEnabled is false)
				VisualStateManager.GoToState(this, DisabledStateName, true);
		}

		private void SetPathData(string partName, string pathData, FrameworkElement element)
		{
			if (string.IsNullOrEmpty(pathData))
				return;

			var geometry = (Geometry)XamlReader.Load(
				$"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pathData}</Geometry>");

			if (GetTemplateChild(partName) is Path path)
			{
				path.Data = geometry;
				path.Width = element.Width;
				path.Height = element.Height;
			}
		}
	}
}
