using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Files.Uwp.Extensions
{
    public static class KeyboardAcceleratorExtensions
    {
        public static bool CheckIsPressed(this KeyboardAccelerator keyboardAccelerator)
        {
            return App.Window.CoreWindow.GetKeyState(keyboardAccelerator.Key).HasFlag(CoreVirtualKeyStates.Down) && // check if the main key is pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu) || App.Window.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down)) && // check if menu (alt) key is a modifier, and if so check if it's pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift) || App.Window.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) && // check if shift key is a modifier, and if so check if it's pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control) || App.Window.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)); // check if ctrl key is a modifier, and if so check if it's pressed
        }
    }
}