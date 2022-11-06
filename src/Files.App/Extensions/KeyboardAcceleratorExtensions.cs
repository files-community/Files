using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Extensions
{
	public static class KeyboardAcceleratorExtensions
	{
		public static bool CheckIsPressed(this KeyboardAccelerator keyboardAccelerator)
		{
			return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(keyboardAccelerator.Key).HasFlag(CoreVirtualKeyStates.Down) && // check if the main key is pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down)) && // check if menu (alt) key is a modifier, and if so check if it's pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) && // check if shift key is a modifier, and if so check if it's pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)); // check if ctrl key is a modifier, and if so check if it's pressed
		}
	}
}