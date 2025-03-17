// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Files.App.UITests.Data
{
	class TestSidebarModel : ISidebarItemModel
	{
		public object? Children => null;

		public IconSource? IconSource { get; set; }

		public bool IsExpanded { get; set; }

		public required string Text { get; set; }

		public object ToolTip => "";

		public bool PaddedItem => false;

		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
