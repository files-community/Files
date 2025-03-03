// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Parameters;
using Files.App.Utils;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

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
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				HashesViewModel = new(listedItem);

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

		private void HashInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			string? matchingAlgorithm = null;

			try
			{
				matchingAlgorithm = HashesViewModel.FindMatchingAlgorithm(HashInputTextBox.Text);
			}
			catch (ArgumentNullException)
			{
				return;
			}

			if (string.IsNullOrEmpty(matchingAlgorithm))
			{
				HashMatchInfoBar.Severity = InfoBarSeverity.Error;
				HashMatchInfoBar.Title = Strings.HashesDoNotMatch.GetLocalizedResource();
				HashMatchInfoBar.IsOpen = true;
				return;
			}
			else
			{
				HashMatchInfoBar.Severity = InfoBarSeverity.Success;
				HashMatchInfoBar.Title = string.Format(Strings.HashesMatch.GetLocalizedResource(), matchingAlgorithm);
				HashMatchInfoBar.IsOpen = true;
				return;
			}
		}


		private async void CompareFileButton_Click(object sender, RoutedEventArgs e)
		{
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
			picker.FileTypeFilter.Add("*");
			WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Instance.WindowHandle);

			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				var selectedFileHash = await HashesViewModel.CalculateSHA384HashAsync(file.Path);
				var currentFileHash = HashesViewModel.Hashes.FirstOrDefault(h => h.Algorithm == "SHA384")?.HashValue;
				HashInputTextBox.Text = selectedFileHash;
				if (selectedFileHash == currentFileHash)
				{
					HashMatchInfoBar.Severity = InfoBarSeverity.Success; // Check mark
					HashMatchInfoBar.Title = Strings.HashesMatch.GetLocalizedResource();
				}
				else
				{
					HashMatchInfoBar.Severity = InfoBarSeverity.Error; // Cross mark
					HashMatchInfoBar.Title = Strings.HashesMatch.GetLocalizedResource();
				}

				HashMatchInfoBar.IsOpen = true;
			}
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
