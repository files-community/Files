using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Files.App.Keyboard
{
	public interface IKeyboardManager
	{
		IKeyboardAction this[KeyboardActionCodes code] { get; }
		IKeyboardAction this[HotKey hotKey] { get; }

		HotKeyStatus GetStatus(HotKey hotKey);
		void SetHotKey(KeyboardActionCodes code, HotKey hotKey);

		void Initialize(IEnumerable<IKeyboardAction> actions);

		void RegisterKeyboard(UIElement element);
		void FillMenu(UIElement element, KeyboardActionCodes code);
	}
}
