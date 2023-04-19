using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class CustomizationPage : BasePropertiesPage
	{
		private CustomizationViewModel CustomizationViewModel { get; set; }

		public CustomizationPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			CustomizationViewModel = new(AppInstance, BaseProperties);
		}

		public override Task<bool> SaveChangesAsync()
			=> Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
