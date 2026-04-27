// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;

namespace Files.App.Extensions
{
	public static class SingleClickOpenModeExtensions
	{
		/// <summary>
		/// Determines whether an item should be opened with a single click for the given input device.
		/// </summary>
		/// <param name="mode">The configured single-click mode.</param>
		/// <param name="deviceType">The pointer device type that triggered the action, or <see langword="null"/> if unknown (e.g., a selection change). Unknown is treated as mouse-like.</param>
		public static bool ShouldOpenWithSingleClick(this SingleClickOpenMode mode, PointerDeviceType? deviceType)
		{
			return mode switch
			{
				SingleClickOpenMode.Always => true,
				SingleClickOpenMode.OnlyForMouse => deviceType is null or PointerDeviceType.Mouse or PointerDeviceType.Pen,
				SingleClickOpenMode.OnlyForTouch => deviceType is PointerDeviceType.Touch,
				_ => false,
			};
		}
	}
}
