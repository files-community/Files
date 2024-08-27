// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class FlattenToRootAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> "FlattenToRoot".GetLocalizedResource();

		public string Description
			=> "FlattenToRootDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Folder");

		public bool IsExecutable =>
			GeneralSettingsService.ShowFlattenOptions &&
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder);

		public FlattenToRootAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems is null)
				return;

			var optionsDialog = new ContentDialog()
			{
				Title = "FlattenFolderDialogTitle".GetLocalizedResource(),
				Content = "FlattenFolderDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Flatten".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				optionsDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await optionsDialog.TryShowAsync();
			if (result != ContentDialogResult.Primary)
				return;

			await Task.WhenAll(context.SelectedItems.Select(item => FlattenFolderAsync(item.ItemPath)));
		}

		private Task FlattenFolderAsync(string folderPath)
		{
			var containedFolders = Directory.GetDirectories(folderPath);
			var containedFiles = Directory.GetFiles(folderPath);

			foreach (var containedFolder in containedFolders)
			{
				FlattenFolderAsync(containedFolder);

				var folderName = Path.GetFileName(containedFolder);
				var destinationPath = Path.Combine(context?.SelectedItem?.ItemPath ?? string.Empty, folderName);

				if (Directory.Exists(destinationPath))
					continue;

				try
				{
					Directory.Move(containedFolder, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Folder '{folderName}' already exists in the destination folder.");
				}
			}

			foreach (var containedFile in containedFiles)
			{
				var fileName = Path.GetFileName(containedFile);
				var destinationPath = Path.Combine(context?.SelectedItem?.ItemPath ?? string.Empty, fileName);

				if (File.Exists(destinationPath))
					continue;

				try
				{
					File.Move(containedFile, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Failed to move file '{fileName}'.");
				}
			}

			if (Directory.GetFiles(folderPath).Length == 0 && Directory.GetDirectories(folderPath).Length == 0)
			{
				try
				{
					Directory.Delete(folderPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Failed to delete folder '{folderPath}'.");
				}
			}

			return Task.CompletedTask;
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
