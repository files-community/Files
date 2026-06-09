// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using SevenZip;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseDecompressArchiveAction : BaseUIAction, IAction
	{
		protected readonly IContentPageContext context;
		protected IStorageArchiveService StorageArchiveService { get; } = Ioc.Default.GetRequiredService<IStorageArchiveService>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public virtual ActionCategory Category
			=> ActionCategory.Archive;

		public virtual HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			(IsContextPageTypeAdaptedToCommand() &&
			CanDecompressSelectedItems() ||
			CanDecompressInsideArchive()) &&
			UIHelpers.CanShowDialog;

		public BaseDecompressArchiveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.ReleaseNotes &&
				context.PageType != ContentPageTypes.Settings &&
				context.PageType != ContentPageTypes.None;
		}

		protected async Task DecompressArchiveHereAsync(bool smart = false)
		{
			if (context.SelectedItems.Count is 0)
				return;

			var selectedItems = context.SelectedItems.ToList();
			var currentFolderPath = context.ShellPage?.ShellViewModel.CurrentFolder?.ItemPath ?? string.Empty;
			BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(currentFolderPath);

			foreach (var selectedItem in selectedItems)
			{
				var password = string.Empty;
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);

				if (archive?.Path is null)
					return;

				if (await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncryptedAsync(archive.Path)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};

					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					if (decompressArchiveViewModel.Password is not null)
						password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				BaseStorageFolder? destinationFolder = null;

				var isMultipleItems = await FilesystemTasks.Wrap(async () =>
				{
					using SevenZipExtractor? zipFile = await StorageArchiveService.GetSevenZipExtractorAsync(archive.Path);
					if (zipFile is null)
						return true;

					static ReadOnlySpan<char> GetFirstMeaningfulSegment(ReadOnlySpan<char> path)
					{
						while (!path.IsEmpty)
						{
							while (!path.IsEmpty && (path[0] == '/' || path[0] == '\\'))
								path = path[1..];

							if (path.IsEmpty)
								break;

							int sep = path.IndexOfAny('/', '\\');
							ReadOnlySpan<char> seg = sep < 0 ? path : path[..sep];

							path = sep < 0 ? ReadOnlySpan<char>.Empty : path[(sep + 1)..];

							if (seg.SequenceEqual(".") || seg.SequenceEqual(".."))
								continue;

							return seg;
						}

						return default;
					}

					string? firstTopLevel = null;
					foreach (var file in zipFile.ArchiveFileData)
					{
						var segment = GetFirstMeaningfulSegment(file.FileName);
						if (segment.IsEmpty)
							continue;

						if (firstTopLevel is null)
							firstTopLevel = segment.ToString();
						else if (!segment.SequenceEqual(firstTopLevel))
							return true;
					}

					return false;
				});

				if (smart && currentFolder is not null && isMultipleItems)
				{
					destinationFolder =
						await FilesystemTasks.Wrap(() =>
							currentFolder.CreateFolderAsync(
								SystemIO.Path.GetFileNameWithoutExtension(archive.Path),
								CreationCollisionOption.GenerateUniqueName).AsTask());
				}
				else
				{
					destinationFolder = currentFolder;
				}

				// Operate decompress
				var result = await FilesystemTasks.Wrap(() =>
					StorageArchiveService.DecompressAsync(selectedItem.ItemPath, destinationFolder?.Path ?? string.Empty, password));
			}
		}

		protected virtual bool CanDecompressInsideArchive()
		{
			return false;
		}

		protected virtual bool CanDecompressSelectedItems()
		{
			return StorageArchiveService.CanDecompress(context.SelectedItems);
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
