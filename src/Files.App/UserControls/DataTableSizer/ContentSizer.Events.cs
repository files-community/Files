// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.DataTableSizer
{
	public partial class ContentSizer
	{
		/// <inheritdoc/>
		protected override void OnLoaded(RoutedEventArgs e)
		{
			if (TargetControl == null)
			{
				TargetControl = this.FindAscendant<FrameworkElement>();
			}
		}

		private double _currentSize;

		/// <inheritdoc/>
		protected override void OnDragStarting()
		{
			if (TargetControl != null)
			{
				_currentSize =
					Orientation == Orientation.Vertical ?
						TargetControl.ActualWidth :
						TargetControl.ActualHeight;
			}
		}

		/// <inheritdoc/>
		protected override bool OnDragHorizontal(double horizontalChange)
		{
			if (TargetControl == null)
			{
				return true;
			}

			horizontalChange = IsDragInverted ? -horizontalChange : horizontalChange;

			if (!IsValidWidth(TargetControl, _currentSize + horizontalChange, ActualWidth))
			{
				return false;
			}

			TargetControl.Width = _currentSize + horizontalChange;

			return true;
		}

		/// <inheritdoc/>
		protected override bool OnDragVertical(double verticalChange)
		{
			if (TargetControl == null)
			{
				return false;
			}

			verticalChange = IsDragInverted ? -verticalChange : verticalChange;

			if (!IsValidHeight(TargetControl, _currentSize + verticalChange, ActualHeight))
			{
				return false;
			}

			TargetControl.Height = _currentSize + verticalChange;

			return true;
		}
	}
}
