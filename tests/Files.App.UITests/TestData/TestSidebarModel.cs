using Files.App.Controls;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Files.App.UITests.TestData
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
