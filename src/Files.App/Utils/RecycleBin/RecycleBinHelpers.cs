// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Utils.RecycleBin
{
	public static class RecycleBinHelpers
	{
		private static readonly StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		private static readonly Regex recycleBinPathRegex = new(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		private static readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
		{
			return (await Win32Shell.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
		}

		public static ulong GetSize()
		{
			return (ulong)Win32Shell.QueryRecycleBin().BinSize;
		}

		public static async Task<bool> IsRecycleBinItem(IStorageItem item)
		{
			List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
			return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
		}

		public static async Task<bool> IsRecycleBinItem(string path)
		{
			List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
			return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
		}

		public static bool IsPathUnderRecycleBin(string path)
		{
			return !string.IsNullOrWhiteSpace(path) && recycleBinPathRegex.IsMatch(path);
		}

		public static async Task EmptyRecycleBinAsync()
		{
			// Display confirmation dialog
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				ConfirmEmptyBinDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			// If the operation is approved by the user
			if (userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy is DeleteConfirmationPolicies.Never ||
				await ConfirmEmptyBinDialog.TryShowAsync() == ContentDialogResult.Primary)
			{

				var banner = StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.InProgress);

				bool bResult = await Task.Run(() => Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI).Succeeded);

				_statusCenterViewModel.RemoveItem(banner);

				if (bResult)
					StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.Success);
				else
					StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.Failed);
			}
		}

		public static async Task RestoreRecycleBinAsync()
		{
			var confirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				confirmEmptyBinDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult result = await confirmEmptyBinDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				try
				{
					Vanara.Windows.Shell.RecycleBin.RestoreAll();
				}
				catch (Exception)
				{
					var errorDialog = new ContentDialog()
					{
						Title = "FailedToRestore".GetLocalizedResource(),
						PrimaryButtonText = "OK".GetLocalizedResource(),
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await errorDialog.TryShowAsync();
				}
			}
		}

		public static async Task RestoreSelectionRecycleBinAsync(IShellPage associatedInstance)
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),
				Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), associatedInstance.SlimContentPage.SelectedItems.Count),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				ConfirmEmptyBinDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult result = await ConfirmEmptyBinDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
				await RestoreItemAsync(associatedInstance);
		}

		public static async Task<bool> HasRecycleBin(string? path)
		{
			if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return false;

			var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

			return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
		}

		public static bool RecycleBinHasItems()
		{
			return Win32Shell.QueryRecycleBin().NumItems > 0;
		}

		public static async Task RestoreItemAsync(IShellPage associatedInstance)
		{
			var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Where(x => x is RecycleBinItem).Select((item) => new
			{
				Source = StorageHelpers.FromPathAndType(
					item.ItemPath,
					item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
				Dest = ((RecycleBinItem)item).ItemOriginalPath
			});
			await associatedInstance.FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
		}

		public static async Task DeleteItemAsync(IShellPage associatedInstance)
		{
			var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
				item.ItemPath,
				item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
			await associatedInstance.FilesystemHelpers.DeleteItemsAsync(items, userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, false, true);
		}
	}
}