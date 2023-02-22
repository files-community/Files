using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Settings
{
	public sealed partial class About : Page
	{
		public AboutViewModel ViewModel
		{
			get => (AboutViewModel)DataContext;
			set => DataContext = value;
		}

		public About()
		{
			InitializeComponent();

			ViewModel = new AboutViewModel();
		}
	}
}