// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Files.App.Views.LayoutModes;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Parameters
{
	public class ColumnParam : NavigationArguments
	{
		public int Column { get; set; }

		public ListView ListView { get; set; }

		public ColumnViewBase? Source { get; set; }
	}
}
