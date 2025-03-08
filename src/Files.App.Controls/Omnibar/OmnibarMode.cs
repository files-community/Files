// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Controls
{
	public partial class OmnibarMode : Control
	{
		private const string ModeClickBorder = "PART_ModeClickBorder";
		private const string InputTextBox = "PART_InputTextBox";

		private Border? _modeClickArea;
		private TextBox? _inputTextBox;

		private bool _isHoveredOver;
		private bool _isPressed;

		public OmnibarMode()
		{
			DefaultStyleKey = typeof(OmnibarMode);
		}

		protected override void OnApplyTemplate()
		{
			_modeClickArea = GetTemplateChild(ModeClickBorder) as Border
				?? throw new MissingFieldException($"Could not find {ModeClickBorder} in the given {nameof(OmnibarMode)}'s style.");
			_inputTextBox = GetTemplateChild(InputTextBox) as TextBox
				?? throw new MissingFieldException($"Could not find {InputTextBox} in the given {nameof(OmnibarMode)}'s style.");

			UpdateVisualStates();

			_modeClickArea.PointerEntered += OmnibarMode_PointerEntered;
			_modeClickArea.PointerPressed += OmnibarMode_PointerPressed;
			_modeClickArea.PointerReleased += OmnibarMode_PointerReleased;
			_modeClickArea.PointerExited += OmnibarMode_PointerExited;

			base.OnApplyTemplate();
		}

		private void UpdateVisualStates()
		{
			VisualStateManager.GoToState(
				this,
				_isPressed ? "PointerPressed" : _isHoveredOver ? "PointerOver" : "PointerNormal",
				true);

			if (IsDefault && Host is not null)
			{
				VisualStateManager.GoToState(this, "Visible",true);
				Host.ColumnDefinitions[Host.Children.IndexOf(this)].Width = new(1, GridUnitType.Star);
			}
		}

		// Events

		private void OmnibarMode_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_isHoveredOver = true;
			_isPressed = false;
			UpdateVisualStates();
		}

		private void OmnibarMode_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_isHoveredOver = false;
			_isPressed = true;
			UpdateVisualStates();
		}

		private void OmnibarMode_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			_isHoveredOver = true;
			_isPressed = false;
			UpdateVisualStates();
		}

		private void OmnibarMode_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_isHoveredOver = _isPressed = false;
			UpdateVisualStates();
		}
	}
}
