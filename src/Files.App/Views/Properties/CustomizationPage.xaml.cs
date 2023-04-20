using Files.App.DataModels;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

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
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			base.OnNavigatedTo(e);

			CustomizationViewModel = new(AppInstance, BaseProperties, parameter.AppWindow);
		}

		public override Task<bool> SaveChangesAsync()
			=> Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
