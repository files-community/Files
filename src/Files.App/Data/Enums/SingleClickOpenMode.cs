// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	public enum SingleClickOpenMode
	{
		/// <summary>
		/// Never open items with a single click.
		/// </summary>
		Never,

		/// <summary>
		/// Open items with a single click only when using touch input.
		/// </summary>
		OnlyForTouch,

		/// <summary>
		/// Open items with a single click only when using a mouse or pen.
		/// </summary>
		OnlyForMouse,

		/// <summary>
		/// Always open items with a single click.
		/// </summary>
		Always,
	}
}
