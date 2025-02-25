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
			=> CompareHashes();

		private void CompareHashes()
		{
			var hashToCompare = HashInputTextBox.Text;

			if (string.IsNullOrEmpty(hashToCompare))
			{
				HashMatchIcon.Visibility = Visibility.Collapsed;
				return;
			}

			foreach (var hashInfo in HashesViewModel.Hashes)
			{
				if (hashInfo.HashValue != null && hashInfo.HashValue.Equals(hashToCompare, StringComparison.OrdinalIgnoreCase))
				{
					HashMatchIcon.Glyph = "\uE73E"; // Check mark
					//HashMatchIcon.Foreground = new SolidColorBrush(Colors.Green);
					ToolTipService.SetToolTip(HashMatchIcon, string.Format(Strings.HashesMatch.GetLocalizedResource(), hashInfo.Algorithm));
					HashMatchIcon.Visibility = Visibility.Visible;
					return;
				}
			}

			HashMatchIcon.Glyph = "\uE711"; // Cross mark
			//HashMatchIcon.Foreground = new SolidColorBrush(Colors.Red);
			ToolTipService.SetToolTip(HashMatchIcon, Strings.HashesDoNotMatch.GetLocalizedResource());
			HashMatchIcon.Visibility = Visibility.Visible;
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
				var selectedFileHash = await CalculateSHA384HashAsync(file.Path);
				var currentFileHash = HashesViewModel.Hashes.FirstOrDefault(h => h.Algorithm == "SHA384")?.HashValue;

				if (selectedFileHash == currentFileHash)
				{
					HashMatchIcon.Glyph = "\uE73E"; // Check mark
					ToolTipService.SetToolTip(HashMatchIcon, Strings.HashesMatch.GetLocalizedResource());
				}
				else
				{
					HashMatchIcon.Glyph = "\uE711"; // Cross mark
					ToolTipService.SetToolTip(HashMatchIcon, Strings.HashesMatch.GetLocalizedResource());
				}

				HashMatchIcon.Visibility = Visibility.Visible;
			}
		}

		private async Task<string> CalculateSHA384HashAsync(string filePath)
		{
			using (var stream = File.OpenRead(filePath))
			{
				using (var sha384 = SHA384.Create())
				{
					var hash = await Task.Run(() => sha384.ComputeHash(stream));
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
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
