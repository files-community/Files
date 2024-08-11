using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace Files.App.Actions
{
	internal sealed class FlattenSingleAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "FlattenSingle".GetLocalizedResource();

		public string Description
			=> "FlattenDescription".GetLocalizedResource();

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder;

		public FlattenSingleAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.ShellViewModel is null)
				return;

			var items = context.SelectedItems;

			if (items is null || !items.Any() || items.Any(item => !item.IsFolder))
				return;

			foreach (var item in items)
			{
				var folderPath = item.ItemPath;

				var containedFolders = await Task.Run(() => Directory.GetDirectories(folderPath));
				var containedFiles = await Task.Run(() => Directory.GetFiles(folderPath));

				foreach (var containedFolder in containedFolders)
				{
					var folderName = Path.GetFileName(containedFolder);
					var destinationPath = Path.Combine(context.ShellPage?.ShellViewModel?.CurrentFolder?.ItemPath ?? string.Empty, folderName);

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
					var destinationPath = Path.Combine(context.ShellPage?.ShellViewModel?.CurrentFolder?.ItemPath ?? string.Empty, fileName);

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
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
