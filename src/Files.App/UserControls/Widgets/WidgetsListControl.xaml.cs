using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class WidgetsListControl : UserControl, IDisposable
	{
		public WidgetsListControlViewModel ViewModel { get; set; } = new();

		public WidgetsListControl()
			=> InitializeComponent();

		public void Dispose()
		{
			ViewModel.Dispose();
		}
	}
}
