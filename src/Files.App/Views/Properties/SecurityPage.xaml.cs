// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		private SecurityViewModel SecurityViewModel { get; set; }

		private object _parameter;

		public SecurityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				SecurityViewModel = new(listedItem, np.Window);
			else if (np.Parameter is DriveItem driveItem)
				SecurityViewModel = new(driveItem, np.Window);

			_parameter = e.Parameter;

			base.OnNavigatedTo(e);
		}

		private void OpenSecurityAdvancedPageButton_Click(object sender, RoutedEventArgs e)
		{
			Frame?.Navigate(typeof(SecurityAdvancedPage), _parameter);
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
