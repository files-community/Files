// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;

namespace Files.App.Services
{
	public class RecycleBinService : ITrashService
	{
		private StatusCenterViewModel StatusCenterViewModel { get; } = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly Regex _recycleBinPathRegex = new(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		public ITrashWatcher Watcher { get; }

		public RecycleBinService()
		{
			Watcher ??= new RecycleBinWatcher();
		}

		public bool IsPathUnderRecycleBin(string path)
		{
			return !string.IsNullOrWhiteSpace(path) && _recycleBinPathRegex.IsMatch(path);
		}

		public ulong GetTrashSize()
		{
			return (ulong)GetRecycleBinInfo().BinSize;
		}

		public bool HasItems()
		{
			return GetRecycleBinInfo().NumItems > 0;
		}

		public bool IsTrashed(string path)
		{
			return !string.IsNullOrWhiteSpace(path) && _recycleBinPathRegex.IsMatch(path);
		}

		public async Task<List<ShellFileItem>> GetAllItemsAsync()
		{
			var folderAndItems = await Win32Shell.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue);

			// TODO: Replace with ILocatableStorable
			return folderAndItems.Enumerate;
		}

		public async Task EmptyTrashAsync()
		{
			// Display confirmation dialog
			var confirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = MainWindow.Instance.Content.XamlRoot
			};

			// If the operation is approved by the user
			if (UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy is DeleteConfirmationPolicies.Never ||
				await confirmEmptyBinDialog.TryShowAsync() == ContentDialogResult.Primary)
			{
				var banner = StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.InProgress);

				bool bResult = await Task.Run(() =>
					Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI).Succeeded);

				StatusCenterViewModel.RemoveItem(banner);

				if (bResult)
					StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.Success);
				else
					StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.Failed);
			}
		}

		public async Task RestoreTrashAsync()
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

		public async Task<bool> CanBeTrashed(string path)
		{
			if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return false;

			// Test recycling
			var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

			return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
		}

		private (bool HasRecycleBin, long NumItems, long BinSize) GetRecycleBinInfo(string drive = "")
		{
			Win32API.SHQUERYRBINFO queryBinInfo = new();
			queryBinInfo.cbSize = Marshal.SizeOf(queryBinInfo);

			var res = Win32API.SHQueryRecycleBin(drive, ref queryBinInfo);
			if (res == HRESULT.S_OK)
			{
				var numItems = queryBinInfo.i64NumItems;
				var binSize = queryBinInfo.i64Size;

				return (true, numItems, binSize);
			}
			else
			{
				return (false, 0, 0);
			}
		}
	}
}
