using Files.App.Views.LayoutModes;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views
{
	public class ColumnParam : NavigationArguments
	{
		public int Column { get; set; }

		public ListView ListView { get; set; }

		public ColumnViewBase? Source { get; set; }
	}
}
