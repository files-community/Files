using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class DetailsPage : BasePropertiesPage
	{
		public DetailsViewModel DetailsViewModel { get; }

		public DetailsPage()
		{
			InitializeComponent();

			DetailsViewModel = new();
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			if (BaseProperties is FileProperties fileProps)
				fileProps.GetSystemFileProperties();
		}

		private void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
		{
			ClearPropertiesButtonFlyout.Hide();
		}

		public override async Task<bool> SaveChangesAsync()
			=> await DetailsViewModel.SaveChanges();

		public override void Dispose()
		{
		}
	}
}
