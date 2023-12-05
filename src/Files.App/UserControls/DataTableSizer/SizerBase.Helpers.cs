// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;
using CursorEnum = Microsoft.UI.Input.InputSystemCursorShape;

namespace Files.App.UserControls.DataTableSizer
{
	public partial class SizerBase
	{
		/// <summary>
		/// Check for new requested vertical size is valid or not
		/// </summary>
		/// <param name="target">Target control being resized</param>
		/// <param name="newHeight">The requested new height</param>
		/// <param name="parentActualHeight">The parent control's ActualHeight</param>
		/// <returns>Bool result if requested vertical change is valid or not</returns>
		protected static bool IsValidHeight(FrameworkElement target, double newHeight, double parentActualHeight)
		{
			var minHeight = target.MinHeight;
			if (newHeight < 0 || (!double.IsNaN(minHeight) && newHeight < minHeight))
			{
				return false;
			}

			var maxHeight = target.MaxHeight;
			if (!double.IsNaN(maxHeight) && newHeight > maxHeight)
			{
				return false;
			}

			if (newHeight <= parentActualHeight)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Check for new requested horizontal size is valid or not
		/// </summary>
		/// <param name="target">Target control being resized</param>
		/// <param name="newWidth">The requested new width</param>
		/// <param name="parentActualWidth">The parent control's ActualWidth</param>
		/// <returns>Bool result if requested horizontal change is valid or not</returns>
		protected static bool IsValidWidth(FrameworkElement target, double newWidth, double parentActualWidth)
		{
			var minWidth = target.MinWidth;
			if (newWidth < 0 || (!double.IsNaN(minWidth) && newWidth < minWidth))
			{
				return false;
			}

			var maxWidth = target.MaxWidth;
			if (!double.IsNaN(maxWidth) && newWidth > maxWidth)
			{
				return false;
			}

			if (newWidth <= parentActualWidth)
			{
				return false;
			}

			return true;
		}
	}
}
