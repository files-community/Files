// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Files.App.Helpers
{
	public static class DragZoneHelper
	{
		/// <summary>
		/// Get Scale Adjustment
		/// </summary>
		/// <param name="window"></param>
		/// <returns>scale factor percent</returns>
		public static double GetScaleAdjustment(Window window)
			=> window.Content.XamlRoot.RasterizationScale;

		/// <summary>
		/// Calculate dragging-zones of title bar<br/>
		/// <strong>You MUST transform the rectangles with <see cref="GetScaleAdjustment"/> before calling <see cref="AppWindowTitleBar.SetDragRectangles"/></strong>
		/// </summary>
		/// <param name="viewportWidth"></param>
		/// <param name="dragZoneHeight"></param>
		/// <param name="dragZoneLeftIndent"></param>
		/// <param name="nonDraggingZones"></param>
		/// <returns></returns>
		public static IEnumerable<RectInt32> GetDragZones(int viewportWidth, int dragZoneHeight, int dragZoneLeftIndent, IEnumerable<RectInt32> nonDraggingZones)
		{
			var draggingZonesX = new List<Range> { new(dragZoneLeftIndent, viewportWidth) };
			var draggingZonesY = new List<IEnumerable<Range>> { new[] { new Range(0, dragZoneHeight) } };

			foreach (var nonDraggingZone in nonDraggingZones)
			{
				for (var i = 0; i < draggingZonesX.Count; ++i)
				{
					var x = draggingZonesX[i];
					var y = draggingZonesY[i].ToArray();

					var xSubtrahend = new Range(nonDraggingZone.X, nonDraggingZone.X + nonDraggingZone.Width);
					var ySubtrahend = new Range(nonDraggingZone.Y, nonDraggingZone.Y + nonDraggingZone.Height);

					var xResult = (x - xSubtrahend).ToArray();
					if (xResult.Length is 1 && xResult[0] == x)
						continue;

					var yResult = (y - ySubtrahend).ToArray();

					switch (xResult.Length)
					{
						case 0:
							{
								draggingZonesY[i] = yResult;

								break;
							}
						case 1:
							{
								draggingZonesX.RemoveAt(i);
								draggingZonesY.RemoveAt(i);

								if (xResult[0].Lower == x.Lower)
								{
									draggingZonesY.InsertRange(i, new[] { y, yResult });
									draggingZonesX.InsertRange(i, new[]
									{
										x with { Upper = xResult[0].Upper },
										x with { Lower = xSubtrahend.Lower }
									});
								}
								// xResult[0].Upper == x.Upper
								else
								{
									draggingZonesY.InsertRange(i, new[] { yResult, y });
									draggingZonesX.InsertRange(i, new[]
									{
										x with { Upper = xSubtrahend.Upper },
										x with { Lower = xResult[0].Lower }
									});
								}

								++i;
								
								break;
							}
						case 2:
							{
								draggingZonesX.RemoveAt(i);
								draggingZonesY.RemoveAt(i);
								draggingZonesY.InsertRange(i, new[] { y, yResult, y });
								draggingZonesX.InsertRange(i, new[]
								{
									x with { Upper = xResult[0].Upper },
									xSubtrahend,
									x with { Lower = xResult[1].Lower }
								});

								++i;
								++i;

								break;
							}
					}
				}
			}

			var rects = draggingZonesX
				.SelectMany((rangeX, i) =>
					draggingZonesY[i].Select(rangeY => new RectInt32(rangeX.Lower, rangeY.Lower, rangeX.Distance, rangeY.Distance)))
				.OrderBy(t => t.Y)
				.ThenBy(t => t.X).ToList();

			for (var i = 0; i < rects.Count - 1; ++i)
			{
				var now = rects[i];
				var next = rects[i + 1];

				if (now.Height == next.Height && now.X + now.Width == next.X)
				{
					rects.RemoveRange(i, 2);
					rects.Insert(i, now with { Width = now.Width + next.Width });
				}
			}

			return rects;
		}

		/// <summary>
		/// Set dragging-zones of title bar
		/// </summary>
		/// <param name="window"></param>
		/// <param name="dragZoneHeight"></param>
		/// <param name="dragZoneLeftIndent"></param>
		/// <param name="nonDraggingZones"></param>
		public static void SetDragZones(Window window, int dragZoneHeight = 40, int dragZoneLeftIndent = 0, IEnumerable<RectInt32>? nonDraggingZones = null)
		{
			var hWnd = WindowNative.GetWindowHandle(window);
			var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			var appWindow = AppWindow.GetFromWindowId(windowId);

			var scaleAdjustment = GetScaleAdjustment(window);
			var windowWidth = (int)(appWindow.Size.Width / scaleAdjustment);

			nonDraggingZones ??= Array.Empty<RectInt32>();

#if DEBUG
			// Subtract the toolbar area (center-top in window), only in DEBUG mode.
			nonDraggingZones = nonDraggingZones.Concat(
				new RectInt32[]
				{
					new((windowWidth - DebugToolbarWidth) / 2, 0, DebugToolbarWidth, DebugToolbarHeight)
				});
#endif

			appWindow.TitleBar.SetDragRectangles(
				GetDragZones(windowWidth, dragZoneHeight, dragZoneLeftIndent, nonDraggingZones)
					.Select(rect =>
						new RectInt32(
							(int)(rect.X * scaleAdjustment),
							(int)(rect.Y * scaleAdjustment),
							(int)(rect.Width * scaleAdjustment),
							(int)(rect.Height * scaleAdjustment))
						)
					.ToArray());
		}

		private const int DebugToolbarWidth = 217;

		private const int DebugToolbarHeight = 25;
	}

	file record Range(int Lower, int Upper)
	{
		public int Distance
			=> Upper - Lower;

		private bool Intersects(Range other)
			=> other.Lower <= Upper && other.Upper >= Lower;

		public static IEnumerable<Range> operator -(Range minuend, Range subtrahend)
		{
			if (!minuend.Intersects(subtrahend))
			{
				yield return minuend;
				yield break;
			}
			if (minuend.Lower < subtrahend.Lower)
				yield return minuend with { Upper = subtrahend.Lower };
			if (minuend.Upper > subtrahend.Upper)
				yield return minuend with { Lower = subtrahend.Upper };
		}

		public static IEnumerable<Range> operator -(IEnumerable<Range> minuends, Range subtrahend)
			=> minuends.SelectMany(minuend => minuend - subtrahend);
	}
}
