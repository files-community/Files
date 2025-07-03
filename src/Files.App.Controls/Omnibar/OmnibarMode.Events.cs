﻿// Copyright (c) Files Community
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

			GlobalHelper.WriteDebugStringForOmnibar($"The mouse pointer has entered the UI area of this Mode ({this})");

			VisualStateManager.GoToState(this, "PointerOver", true);
		}

		private void ModeButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			GlobalHelper.WriteDebugStringForOmnibar($"The mouse pointer has been pressed on the UI area of this Mode ({this})");

			VisualStateManager.GoToState(this, "PointerPressed", true);
		}

		private void ModeButton_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			GlobalHelper.WriteDebugStringForOmnibar($"The mouse pointer has been unpressed from the UI area of this Mode ({this})");

			VisualStateManager.GoToState(this, "PointerOver", true);
		}

		private void ModeButton_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			GlobalHelper.WriteDebugStringForOmnibar($"The mouse pointer has moved away from the UI area of this Mode ({this})");

			VisualStateManager.GoToState(this, "PointerNormal", true);
		}

		private void ModeButton_Click(object sender, RoutedEventArgs e)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				return;

			owner.CurrentSelectedMode = this;
		}
	}
}