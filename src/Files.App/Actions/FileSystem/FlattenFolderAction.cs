// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class FlattenFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.FlattenFolder.GetLocalizedResource();

		public string Description
			=> Strings.FlattenFolderDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Folder");

		public bool IsExecutable =>
			GeneralSettingsService.ShowFlattenOptions &&
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItems.Count is 1 &&
			context.SelectedItem is not null &&
			context.SelectedItem.PrimaryItemAttribute is StorageItemTypes.Folder;

		public FlattenFolderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItem is null)
				return;

			var optionsDialog = new ContentDialog()
			{
				Title = Strings.FlattenFolder.GetLocalizedResource(),
				Content = Strings.FlattenFolderDialogContent.GetLocalizedResource(),
				PrimaryButtonText = Strings.Flatten.GetLocalizedResource(),
				SecondaryButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				optionsDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await optionsDialog.TryShowAsync();
			if (result != ContentDialogResult.Primary)
				return;

			var rootPath = context.SelectedItem.ItemPath;
			if (string.IsNullOrWhiteSpace(rootPath))
				return;

			try
			{
				await Task.Run(() => FlattenFolder(rootPath));
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to flatten folder '{FolderPath}'.", LogPathHelper.RedactPath(rootPath));
			}
		}

		private static void FlattenFolder(string path)
		{
			var rootPath = Path.GetFullPath(path);
			FlattenFolderCore(rootPath, rootPath);
		}

		private static void FlattenFolderCore(string rootPath, string currentPath)
		{
			var containedFolders = Directory.GetDirectories(currentPath);
			var containedFiles = Directory.GetFiles(currentPath);

			foreach (var containedFolder in containedFolders)
			{
				var folderName = Path.GetFileName(containedFolder);
				try
				{
					if (!IsUnderRoot(rootPath, containedFolder) || IsReparsePoint(containedFolder))
						continue;

					FlattenFolderCore(rootPath, containedFolder);
					if (!Directory.Exists(containedFolder))
						continue;

					var destinationPath = Path.Combine(rootPath, folderName);
					if (string.Equals(containedFolder, destinationPath, StringComparison.OrdinalIgnoreCase) || Directory.Exists(destinationPath))
						continue;

					Directory.Move(containedFolder, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, "Failed to process folder '{FolderName}'.", LogPathHelper.RedactPath(folderName));
				}
			}

			foreach (var containedFile in containedFiles)
			{
				var fileName = Path.GetFileName(containedFile);
				try
				{
					if (!IsUnderRoot(rootPath, containedFile) || IsReparsePoint(containedFile))
						continue;

					var destinationPath = Path.Combine(rootPath, fileName);
					if (string.Equals(containedFile, destinationPath, StringComparison.OrdinalIgnoreCase) || File.Exists(destinationPath))
						continue;

					File.Move(containedFile, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, $"Failed to move file '{LogPathHelper.RedactPath(fileName)}'.");
				}
			}

			if (!string.Equals(currentPath, rootPath, StringComparison.OrdinalIgnoreCase) &&
				!Directory.EnumerateFileSystemEntries(currentPath).Any())
			{
				try
				{
					Directory.Delete(currentPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, "Failed to delete folder '{FolderPath}'.", LogPathHelper.RedactPath(currentPath));
				}
			}
		}

		private static bool IsReparsePoint(string path)
			=> File.GetAttributes(path).HasFlag(System.IO.FileAttributes.ReparsePoint);

		private static bool IsUnderRoot(string rootPath, string path)
		{
			var relativePath = Path.GetRelativePath(rootPath, Path.GetFullPath(path));
			return !Path.IsPathRooted(relativePath) &&
				relativePath is not ".." &&
				!relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IGeneralSettingsService.ShowFlattenOptions))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
