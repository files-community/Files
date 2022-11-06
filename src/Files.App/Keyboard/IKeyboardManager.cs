using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Files.App.Keyboard
{
	public interface IKeyboardManager
	{
		IKeyboardAction this[KeyboardActionCodes code] { get; }
		IKeyboardAction this[ShortKey shortKey] { get; }

		ShortKeyStatus GetStatus(ShortKey shortKey);
		void SetShortKey(KeyboardActionCodes code, ShortKey shortKey);

		void Initialize(IEnumerable<IKeyboardAction> actions);

		void RegisterKeyboard(UIElement element);
		void FillMenu(UIElement element, KeyboardActionCodes code);
	}
}
