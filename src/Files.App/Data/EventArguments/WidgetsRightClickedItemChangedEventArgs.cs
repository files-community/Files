﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments
{
	public class WidgetsRightClickedItemChangedEventArgs
	{
		public WidgetCardItem? Item { get; set; }

		public CommandBarFlyout? Flyout { get; set; }

		public WidgetsRightClickedItemChangedEventArgs(WidgetCardItem? item = null, CommandBarFlyout? flyout = null)
		{
			Item = item;
			Flyout = flyout;
		}
	}
}
