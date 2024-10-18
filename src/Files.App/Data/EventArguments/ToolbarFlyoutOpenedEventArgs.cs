// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments
{
	public sealed class ToolbarFlyoutOpenedEventArgs
	{
		public MenuFlyout OpenedFlyout { get; set; }
	}
}
