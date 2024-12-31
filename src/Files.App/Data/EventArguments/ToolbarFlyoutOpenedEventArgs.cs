// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public sealed class ToolbarFlyoutOpenedEventArgs
	{
		public MenuFlyout OpenedFlyout { get; set; }
	}
}
