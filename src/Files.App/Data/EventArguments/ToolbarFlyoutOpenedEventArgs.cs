// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public class ToolbarFlyoutOpenedEventArgs
	{
		public MenuFlyout OpenedFlyout { get; set; }
	}
}
