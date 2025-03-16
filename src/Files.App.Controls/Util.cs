using Microsoft.UI.Input;
using System.Reflection;

namespace Files.App.Controls
{
	public static class Util
	{
		/// <summary>
		/// Sets cursor when hovering on a specific element.
		/// </summary>
		/// <param name="uiElement">An element to be changed.</param>
		/// <param name="cursor">Cursor to change.</param>
		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			Type type = typeof(UIElement);

			type.InvokeMember(
				"ProtectedCursor",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null,
				uiElement,
				[cursor]
			);
		}
	}
}
