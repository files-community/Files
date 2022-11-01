using Files.App.ViewModels.SettingsViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.SettingsPages
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

			this.ViewModel = new AboutViewModel();
		}
	}
}