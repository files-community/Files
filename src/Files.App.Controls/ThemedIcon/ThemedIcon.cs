// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Files.App.Controls
{
    /// <summary>
    /// A control for a State and Color aware Icon
    /// </summary>
    public partial class ThemedIcon : Control
    {
        private bool				_isHighContrast;
        private ToggleBehaviors		_toggleBehavior;
        private bool				_ownerToggled;
        private bool				_isEnabled;
        private bool				_isFilled;
        private double				_iconSize;

        private ToggleButton?		ownerToggleButton		= null;
        private AppBarToggleButton? ownerAppBarToggleButton = null;
        private Control?			ownerControl			= null;

        public ThemedIcon()
        {
            DefaultStyleKey = typeof(ThemedIcon);
        }

        protected override void OnApplyTemplate()
        {
            IsEnabledChanged -= OnIsEnabledChanged;
            SizeChanged -= OnSizeChanged;

            base.OnApplyTemplate();

            IsEnabledChanged += OnIsEnabledChanged;
            SizeChanged += OnSizeChanged;

            InitialIconStateValues();
            FindOwnerControlStates();
            UpdateIconContent();
            UpdateIconStates();
            UpdateVisualStates();
        }

        private void UpdateIconContent()
        {
            // Updates PathData and Layers
            UpdateFilledIconPath();
            UpdateOutlineIconPath();
            UpdateLayeredIconContent();
        }

        private void UpdateFilledIconPath()
        {
            // Updates Filled Icon from Path Data
            if (GetTemplateChild(FilledPathIconViewBox) is not Viewbox filledViewBox)
                return;

            SetPathData(FilledIconPath, FilledIconData ?? string.Empty, filledViewBox);
        }

        private void UpdateOutlineIconPath()
        {
            // Updates Outline Icon from Path Data
            if (GetTemplateChild(OutlinePathIconViewBox) is not Viewbox outlineViewBox)
                return;

            SetPathData(OutlineIconPath, OutlineIconData ?? string.Empty, outlineViewBox);
        }

        private void UpdateLayeredIconContent()
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
                        LayerColor = this.Color,
                        Foreground = this.Foreground,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        LayerSize = _iconSize,
                        Width = layer.LayerSize,
                        Height = layer.LayerSize

                    });
            }
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
                path.Width = _iconSize;
                path.Height = _iconSize;
            }
        }

        private void FindOwnerControlStates()
        {
            /*
            // Finds the owner Control and it's Checked and Enabled state

            //
            // Check if owner Control is a ToggleButton
            // Hooks onto Event handlers when IsChecked and IsUnchecked runs
            // Runs the ToggleChanged event, to set initial value, if the ToggleButton's isChecked is true
            //
            // Check if owner Control is an AppBarToggleButton
            // Hooks onto Event handlers when IsChecked and IsUnchecked runs
            // Runs the ToggleChanged event, to set initial value, if the AppBarToggleButton's isChecked is true
            //
            // Gets the owner Control
            // Hooks onto Event handlers when IsEnabledChanged runs
            // Runs the EnabledChanged event to set initial value
            //
            */

            ownerToggleButton = this.FindAscendant<ToggleButton>();

            if (ownerToggleButton != null)
            {
                ownerToggleButton.Checked += OwnerControl_IsCheckedChanged;
                ownerToggleButton.Unchecked += OwnerControl_IsCheckedChanged;

                UpdateOwnerToggle( ownerToggleButton.IsChecked is true);
            }

            ownerAppBarToggleButton = this.FindAscendant<AppBarToggleButton>();

            if (ownerAppBarToggleButton != null)
            {
                ownerAppBarToggleButton.Checked += OwnerControl_IsCheckedChanged;
                ownerAppBarToggleButton.Unchecked += OwnerControl_IsCheckedChanged;

                UpdateOwnerToggle( ownerAppBarToggleButton.IsChecked is true);
            }

            ownerControl = this.FindAscendant<Control>();

            if (ownerControl != null)
            {
                ownerControl.IsEnabledChanged += OwnerControl_IsEnabledChanged;

                EnabledChanged(ownerControl.IsEnabled);
            }
        }

        private void OwnerControl_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            // Responds to owner checked changes
            if (ownerToggleButton is null && ownerAppBarToggleButton is null)
                return;

            if (ownerToggleButton is not null)
                UpdateOwnerToggle( ownerToggleButton.IsChecked is true);
            else if (ownerAppBarToggleButton is not null)
                UpdateOwnerToggle( ownerAppBarToggleButton.IsChecked is true);
        }

        private void OwnerControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Responds to owner control enabled changes
            if (ownerControl is null)
                return;

            EnabledChanged(ownerControl.IsEnabled);
        }

        private void ToggleBehaviorChanged(ToggleBehaviors value)
        {
            // Handles the IsToggled property change
            _toggleBehavior = value;

            UpdateVisualStates();
        }

        private void UpdateOwnerToggle(bool isToggled)
        {
            _ownerToggled = isToggled;

            UpdateVisualStates();
        }

        private void FilledChanged(bool value)
        {
            // Handles the IsToggled property change
            _isFilled = value;

            UpdateVisualStates();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Handles for the derived control's IsEnabled property change
            EnabledChanged((bool)e.NewValue);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Handles resizing of child layers when Width and Height properties change
        }

        private void IconSizePropertyChanged(double value)
        {
            // Code to handle the design time Icon Size changing
            _iconSize = value;

            UpdateVisualStates();
        }


        private void EnabledChanged(bool value)
        {
            // Handles the IsEnabled property change
            _isEnabled = value;

            UpdateVisualStates();
        }

        private void ThemeSettings_OnHighContrastChanged(object sender, bool e)
        {
            HighContrastChanged(e);
        }

        private void HighContrastChanged(bool value)
        {
            // handles HighContrast property change
            _isHighContrast = value;

            UpdateVisualStates();
        }

        private void InitialIconStateValues()
        {
            _isEnabled = IsEnabled;
            _toggleBehavior = ToggleBehavior;
            _isHighContrast = IsHighContrast;
            _iconSize = IconSize;
        }

        private void UpdateIconStates()
        {
            ToggleBehaviorChanged(_toggleBehavior);
            EnabledChanged(_isEnabled);
            HighContrastChanged(_isHighContrast);
        }

        private void UpdateVisualStates()
        {
            // Updates all Icon Visual States.
            UpdateIconTypeStates();
            UpdateIconColorTypeStates();
        }

        private void UpdateIconTypeStates()
        {
            // Handles changes to the IconType and setting the correct Visual States.

            // If ToggleBehavior is Auto, we check for _ownerToggle
			if ( _toggleBehavior == ToggleBehaviors.Auto )
			{
				if ( _ownerToggled is true || _isFilled is true || IsFilled is true )
				{
					VisualStateManager.GoToState( this , FilledTypeStateName , true );
					return;
				}
				else if ( _isHighContrast is true || IsHighContrast is true || _isEnabled is false || IsEnabled is false )
				{
					VisualStateManager.GoToState( this , OutlineTypeStateName , true );
					VisualStateManager.GoToState( this , DisabledStateName , true );
					return;
				}
				else
				{
					if ( IconType == ThemedIconTypes.Layered )
					{
						VisualStateManager.GoToState( this , LayeredTypeStateName , true );
					}
					else
					{
						VisualStateManager.GoToState( this , OutlineTypeStateName , true );
					}
				}
			}
			// If ToggleBehavior is On, we only go to Filled.
			else if ( _toggleBehavior == ToggleBehaviors.On )
			{
				VisualStateManager.GoToState( this , FilledTypeStateName , true );
			}
			// For Off, we don't respond to _ownerToggle at all
			else
			{
				if ( _isFilled is true || IsFilled is true )
				{
					VisualStateManager.GoToState( this , FilledTypeStateName , true );
					return;
				}
				else if ( _isHighContrast is true || IsHighContrast is true || _isEnabled is false || IsEnabled is false )
				{
					VisualStateManager.GoToState( this , OutlineTypeStateName , true );
					VisualStateManager.GoToState( this , DisabledStateName , true );
					return;
				}
				else
				{
					if ( IconType == ThemedIconTypes.Layered )
					{
						VisualStateManager.GoToState( this , LayeredTypeStateName , true );
					}
					else
					{
						VisualStateManager.GoToState( this , OutlineTypeStateName , true );
					}
				}
			}

            VisualStateManager.GoToState(this, EnabledStateName, true);
        }

        private void UpdateIconColorTypeStates()
        {
            // Handles changes to the IconColorType and setting the correct Visual States.

			// First we check the enabled state
            if (_isEnabled is false || IsEnabled is false)
            {
				// If ToggleBehavior is Auto and _ownerToggled is true
				if ( _toggleBehavior == ToggleBehaviors.Auto && _ownerToggled is true )
				{
					VisualStateManager.GoToState( this , DisabledToggleColorStateName , true );
				}
				// If ToggleBehavior is On
				else if ( _toggleBehavior == ToggleBehaviors.On )
				{
					VisualStateManager.GoToState( this , DisabledToggleColorStateName , true );
				}
				// everything else uses Disabled Color
				else
				{
					VisualStateManager.GoToState( this , DisabledColorStateName , true );
				}
            }
			// Everything else is Enabled
            else
            {
				// If ToggleBehavior is Auto and _ownerToggled is true
				if ( _toggleBehavior == ToggleBehaviors.Auto && _ownerToggled is true )
				{
                    VisualStateManager.GoToState(this, ToggleStateName, true);
                }
				// If ToggleBehavior is On
				else if ( _toggleBehavior == ToggleBehaviors.On )
				{
					VisualStateManager.GoToState( this , ToggleStateName , true );
				}
				// everything else uses the appropriate color state
				else
				{
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

                if (GetTemplateChild(LayeredPathCanvas) is Canvas canvas)
                {
                    foreach (var layer in canvas.Children.Cast<ThemedIconLayer>())
                        layer.IconColorType = IconColorType;
                }
            }
        }
    }
}
