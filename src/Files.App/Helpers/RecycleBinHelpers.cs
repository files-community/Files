// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Shell;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class RecycleBinHelpers
	{
		private static readonly OngoingTasksViewModel ongoingTasksViewModel = Ioc.Default.GetRequiredService<OngoingTasksViewModel>();

		private static readonly Regex recycleBinPathRegex = new(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		private static readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
			=> (await Win32Shell.GetShellFolderAsync(CommonPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;

		public static ulong GetSize()
			=> (ulong)Win32Shell.QueryRecycleBin().BinSize;
		
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
			=> !string.IsNullOrWhiteSpace(path) && recycleBinPathRegex.IsMatch(path);

		public static async Task EmptyRecycleBin()
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy is DeleteConfirmationPolicies.Never ||
				await ConfirmEmptyBinDialog.TryShowAsync() == ContentDialogResult.Primary)
			{
				string bannerTitle = "EmptyRecycleBin".GetLocalizedResource();
				var banner =
					ongoingTasksViewModel.PostBanner(
						bannerTitle,
						"EmptyingRecycleBin".GetLocalizedResource(),
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete);

				bool opSucceded = Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI).Succeeded;

				banner.Remove();

				if (opSucceded)
					ongoingTasksViewModel.PostBanner(
						bannerTitle,
						"BinEmptyingSucceded".GetLocalizedResource(),
						100,
						ReturnResult.Success,
						FileOperationType.Delete);
				else
					ongoingTasksViewModel.PostBanner(
						bannerTitle,
						"BinEmptyingFailed".GetLocalizedResource(),
						100,
						ReturnResult.Failed,
						FileOperationType.Delete);
			}
		}

		public static async Task RestoreRecycleBin()
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			ContentDialogResult result = await ConfirmEmptyBinDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
				Vanara.Windows.Shell.RecycleBin.RestoreAll();
		}

		public static async Task RestoreSelectionRecycleBin(IShellPage associatedInstance)
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),
				Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), associatedInstance.SlimContentPage.SelectedItems.Count),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			ContentDialogResult result = await ConfirmEmptyBinDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
				await RestoreItem(associatedInstance);
		}

		public static async Task<bool> HasRecycleBin(string? path)
		{
			if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return false;

			var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

			return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
		}

		public static bool RecycleBinHasItems()
			=> Win32Shell.QueryRecycleBin().NumItems > 0;

		public static async Task RestoreItem(IShellPage associatedInstance)
		{
			var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Where(x => x is RecycleBinItem).Select((item) => new
			{
				Source = StorageHelpers.FromPathAndType(
					item.ItemPath,
					item.PrimaryItemAttribute == StorageItemTypes.File
						? FilesystemItemType.File
						: FilesystemItemType.Directory),
				Dest = ((RecycleBinItem)item).ItemOriginalPath
			});

			await associatedInstance.FilesystemHelpers.RestoreItemsFromTrashAsync(
				items.Select(x => x.Source),
				items.Select(x => x.Dest),
				true);
		}

		public static async Task DeleteItem(IShellPage associatedInstance)
		{
			var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) =>
				StorageHelpers.FromPathAndType(
					item.ItemPath,
					item.PrimaryItemAttribute == StorageItemTypes.File
						? FilesystemItemType.File
						: FilesystemItemType.Directory));

			await associatedInstance.FilesystemHelpers.DeleteItemsAsync(
				items,
				userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy,
				false,
				true);
		}
	}
}
