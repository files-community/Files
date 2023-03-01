using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics;

namespace Files.App.Views.Properties
{
	public sealed partial class HashPage : BasePropertiesPage
	{
		public HashPage()
		{
			InitializeComponent();
		}

		private HashViewModel ViewModel { get; set }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as MainPropertiesPage.PropertyNavParam;
			if (np.navParameter is ListedItem listedItem)
				ViewModel = new(listedItem);

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
		{
			return true;
		}

		public override void Dispose()
		{
		}
	}
}
