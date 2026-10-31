using Microsoft.UI.Input;
using System.Runtime.CompilerServices;

namespace Files.App.Controls
{
	public static class GlobalHelper
	{
		/// <summary>
		/// Sets cursor when hovering on a specific element.
		/// </summary>
		/// <param name="uiElement">An element to be changed.</param>
		/// <param name="cursor">Cursor to change.</param>
		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			SetProtectedCursor(uiElement, cursor);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ProtectedCursor")]
		private static extern void SetProtectedCursor(UIElement uiElement, InputCursor cursor);

		[Conditional("OMNIBAR_DEBUG")]
		public static void WriteDebugStringForOmnibar(string? message)
		{
			Debug.WriteLine($"OMNIBAR DEBUG: [{message}]");
		}
	}
}
