// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class HashesPage : BasePropertiesPage
	{
		private HashesViewModel HashesViewModel { get; set; }

		private bool _cancel;

		public HashesPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			if (parameter.Parameter is ListedItem listedItem)
				HashesViewModel = new(listedItem, parameter.Window.AppWindow);

			base.OnNavigatedTo(e);
		}

		private void CopyHashButton_Click(object sender, RoutedEventArgs e)
		{
			var item = (HashInfoItem)(((Button)sender).DataContext);

			var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dp.SetText(item.HashValue);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
		}

		private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			_cancel = true;
		}

		private void MenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs e)
		{
			e.Cancel = _cancel;
			_cancel = false;
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
			=> Dispose();

		public async override Task<bool> SaveChangesAsync()
		{
			return true;
		}

		public override void Dispose()
		{
			HashesViewModel.Dispose();
		}
	}
}
