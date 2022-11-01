using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;

namespace Files.App.Helpers
{
	public class RecycleBinHelpers
	{
		#region Private Members

		private static readonly Regex recycleBinPathRegex = new Regex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		#endregion Private Members

		public async Task<List<ShellFileItem>> EnumerateRecycleBin()
		{
			return (await Win32Shell.GetShellFolderAsync(CommonPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
		}

		public async Task<bool> IsRecycleBinItem(IStorageItem item)
		{
			List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
			return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
		}

		public async Task<bool> IsRecycleBinItem(string path)
		{
			List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
			return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
		}

		public bool IsPathUnderRecycleBin(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return false;
			return recycleBinPathRegex.IsMatch(path);
		}

		public static async Task S_EmptyRecycleBin()
		{
			await new RecycleBinHelpers().EmptyRecycleBin();
		}

		public async Task EmptyRecycleBin()
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};
			ContentDialogResult result = await this.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				string bannerTitle = "EmptyRecycleBin".GetLocalizedResource();
				var banner = App.OngoingTasksViewModel.PostBanner(
					bannerTitle,
					"EmptyingRecycleBin".GetLocalizedResource(),
					0.0f,
					ReturnResult.InProgress,
					FileOperationType.Delete);

				bool opSucceded = Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI).Succeeded;
				banner.Remove();
				if (opSucceded)
					App.OngoingTasksViewModel.PostBanner(
						bannerTitle,
						"BinEmptyingSucceded".GetLocalizedResource(),
						100.0f,
						ReturnResult.Success,
						FileOperationType.Delete);
				else
					App.OngoingTasksViewModel.PostBanner(
						bannerTitle,
						"BinEmptyingFailed".GetLocalizedResource(),
						100.0f,
						ReturnResult.Failed,
						FileOperationType.Delete);
			}
		}

		public static async Task S_RestoreRecycleBin(IShellPage associatedInstance)
		{
			await new RecycleBinHelpers().RestoreRecycleBin(associatedInstance);
		}

		public async Task RestoreRecycleBin(IShellPage associatedInstance)
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			ContentDialogResult result = await this.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				associatedInstance.SlimContentPage.ItemManipulationModel.SelectAllItems();
				await this.RestoreItem(associatedInstance);
			}
		}

		public static async Task S_RestoreSelectionRecycleBin(IShellPage associatedInstance)
		{
			await new RecycleBinHelpers().RestoreSelectionRecycleBin(associatedInstance);
		}

		public async Task RestoreSelectionRecycleBin(IShellPage associatedInstance)
		{
			var ConfirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),
				Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), associatedInstance.SlimContentPage.SelectedItems.Count),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			ContentDialogResult result = await this.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

			if (result == ContentDialogResult.Primary)
				await this.RestoreItem(associatedInstance);
		}

		//WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		public async Task<bool> HasRecycleBin(string path)
		{
			if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return false;

			var result = await FileOperationsHelpers.TestRecycleAsync(path.Split("|"));

			return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
		}

		public bool RecycleBinHasItems()
		{
			return Win32Shell.QueryRecycleBin().NumItems > 0;
		}

		public static async Task S_RestoreItem(IShellPage associatedInstance)
		{
			await new RecycleBinHelpers().RestoreItem(associatedInstance);
		}

		private async Task RestoreItem(IShellPage associatedInstance)
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

		public static async Task S_DeleteItem(IShellPage associatedInstance)
		{
			await new RecycleBinHelpers().DeleteItem(associatedInstance);
		}

		public virtual async Task DeleteItem(IShellPage associatedInstance)
		{
			var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
				item.ItemPath,
				item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
			await associatedInstance.FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
		}
	}
}