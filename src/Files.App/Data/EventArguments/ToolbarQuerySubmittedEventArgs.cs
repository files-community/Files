﻿// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public sealed class ToolbarQuerySubmittedEventArgs
	{
		public string QueryText { get; set; } = null;
	}
}
