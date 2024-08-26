// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;

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

		public bool IsExecutable =>
			GeneralSettingsService.ShowFlattenOptions &&
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder;

		public FlattenToRootAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.ShellViewModel is null)
				return Task.CompletedTask;

			var items = context.SelectedItems;

			if (items is null || !items.Any() || items.Any(item => !item.IsFolder))
				return Task.CompletedTask;

			foreach (var item in items)
				FlattenFolderAsync(item.ItemPath);

			return Task.CompletedTask;
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
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
