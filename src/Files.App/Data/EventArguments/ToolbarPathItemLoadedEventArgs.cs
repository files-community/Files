// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments
{
	public sealed class ToolbarPathItemLoadedEventArgs
	{
		public MenuFlyout OpenedFlyout { get; set; }

		public PathBoxItem Item { get; set; }
	}
}
