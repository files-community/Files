using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class LibraryPage : BasePropertiesPage
	{
		public LibraryViewModel LibraryViewModel { get; }

		public LibraryPage()
		{
			InitializeComponent();

			LibraryViewModel = new();
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			LibraryViewModel.Initialize(BaseProperties);
		}

		public override async Task<bool> SaveChangesAsync()
			=> await LibraryViewModel.SaveChanges(BaseProperties);

		public override void Dispose()
		{
		}
	}
}
