using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class WidgetsListControl : UserControl, IDisposable
	{
		public WidgetsListControlViewModel ViewModel
		{
			get => (WidgetsListControlViewModel)DataContext;
			set => DataContext = value;
		}

		public WidgetsListControl()
		{
			this.InitializeComponent();

			this.ViewModel = new WidgetsListControlViewModel();
		}

		public void Dispose()
		{
			ViewModel?.Dispose();
		}
	}
