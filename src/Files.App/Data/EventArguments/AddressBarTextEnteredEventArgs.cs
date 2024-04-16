﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public sealed class AddressBarTextEnteredEventArgs
	{
		public AutoSuggestBox AddressBarTextField { get; set; }
	}
}
