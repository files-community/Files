// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	public partial class OmnibarMode
	{
		private void ModeButton_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			VisualStateManager.GoToState(this, "PointerOver", true);
		}

		private void ModeButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			VisualStateManager.GoToState(this, "PointerPressed", true);
		}

		private void ModeButton_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			VisualStateManager.GoToState(this, "PointerOver", true);

			// Change the current mode
			owner.ChangeMode(this);
		}

		private void ModeButton_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "PointerNormal", true);
		}
	}
}
