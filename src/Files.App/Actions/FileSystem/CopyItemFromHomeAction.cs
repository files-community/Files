// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CopyItemFromHomeAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IHomePageContext HomePageContext;

		public string Label
			=> Strings.Copy.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemDescription.GetLocalizedFormatResource(1);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Copy");
		public bool IsExecutable
			=> GetIsExecutable();

		public CopyItemFromHomeAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			HomePageContext = Ioc.Default.GetRequiredService<IHomePageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (HomePageContext.RightClickedItem is null)
				return;

			var item = HomePageContext.RightClickedItem;
			var itemPath = item.Path;

			if (string.IsNullOrEmpty(itemPath))
				return;

			try
			{
				var dataPackage = new DataPackage() { RequestedOperation = DataPackageOperation.Copy };
				IStorageItem? storageItem = null;

				var folderResult = await context.ShellPage?.ShellViewModel?.GetFolderFromPathAsync(itemPath)!;
				if (folderResult)
					storageItem = folderResult.Result;

				if (storageItem is null)
				{
					await CopyPathFallback(itemPath);
					return;
				}

				if (storageItem is SystemStorageFolder or SystemStorageFile)
				{
					var standardItems = await new[] { storageItem }.ToStandardStorageItemsAsync();
					if (standardItems.Any())
						storageItem = standardItems.First();
				}

				dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
				dataPackage.SetStorageItems(new[] { storageItem }, false);

				Clipboard.SetContent(dataPackage);
			}
			catch (Exception ex)
			{
				if ((FileSystemStatusCode)ex.HResult is FileSystemStatusCode.Unauthorized)
				{
					await CopyPathFallback(itemPath);
					return;
				}

			}
		}

		private bool GetIsExecutable()
		{
			var item = HomePageContext.RightClickedItem;

			return HomePageContext.IsAnyItemRightClicked
				&& item is not null
				&& !IsNonCopyableLocation(item);
		}

		private async Task CopyPathFallback(string path)
		{
			try
			{
				await FileOperationsHelpers.SetClipboard(new[] { path }, DataPackageOperation.Copy);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to copy path to clipboard.");
			}
		}

		private bool IsNonCopyableLocation(WidgetCardItem item)
		{
			if (string.IsNullOrEmpty(item.Path))
				return true;

			return string.Equals(item.Path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(item.Path, Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(item.Path, Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase);
		}
	}
}
