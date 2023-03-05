using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Settings
{
	public sealed partial class AboutPage : Page
	{
		public AboutViewModel ViewModel
		{
			get => (AboutViewModel)DataContext;
			set => DataContext = value;
		}

		public AboutPage()
		{
			InitializeComponent();

			ViewModel = new AboutViewModel();
		}
	}
}
