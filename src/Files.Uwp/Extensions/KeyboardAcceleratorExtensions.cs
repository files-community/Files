using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Files.Extensions
{
    public static class KeyboardAcceleratorExtensions
    {
        public static bool CheckIsPressed(this KeyboardAccelerator keyboardAccelerator)
        {
            return Window.Current.CoreWindow.GetKeyState(keyboardAccelerator.Key).HasFlag(CoreVirtualKeyStates.Down) && // check if the main key is pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu) || Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down)) && // check if menu (alt) key is a modifier, and if so check if it's pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift) || Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) && // check if shift key is a modifier, and if so check if it's pressed
                (!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control) || Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)); // check if ctrl key is a modifier, and if so check if it's pressed
        }
    }
}