// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Files.App.Views.Layouts;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Parameters
{
	public class ColumnParam : NavigationArguments
	{
		public int Column { get; set; }

		public ListView ListView { get; set; }

		public ColumnLayoutPage? Source { get; set; }
	}
}
