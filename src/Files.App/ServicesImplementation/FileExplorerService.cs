using Files.App.Storage.WindowsStorage;
using Files.Core.Services;
using Files.Sdk.Storage.LocatableStorage;
using Files.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IFileExplorerService"/>
	internal sealed class FileExplorerService : IFileExplorerService
	{
		/// <inheritdoc/>
		public Task OpenAppFolderAsync(CancellationToken cancellationToken = default)
			=> Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask(cancellationToken);

		/// <inheritdoc/>
		public Task OpenInFileExplorerAsync(ILocatableFolder folder, CancellationToken cancellationToken = default)
			=> Launcher.LaunchFolderPathAsync(folder.Path).AsTask(cancellationToken);

		/// <inheritdoc/>
		public async Task<ILocatableFile?> PickSingleFileAsync(IEnumerable<string>? filter, CancellationToken cancellationToken = default)
		{
			var filePicker = InitializeWithWindow(new FileOpenPicker());

			if (filter is not null)
			{
				filePicker.FileTypeFilter.EnumeratedAdd(filter);
			}
			else
			{
				filePicker.FileTypeFilter.Add("*");
			}

			var fileTask = filePicker.PickSingleFileAsync().AsTask(cancellationToken);
			var file = await fileTask;

			return file is null ? null : new WindowsStorageFile(file);
		}

		// WINUI3
		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		/// <inheritdoc/>
		public async Task<ILocatableFolder?> PickSingleFolderAsync(CancellationToken cancellationToken = default)
		{
			var folderPicker = InitializeWithWindow(new FolderPicker());

			folderPicker.FileTypeFilter.Add("*");

			var folderTask = folderPicker.PickSingleFolderAsync().AsTask(cancellationToken);
			var folder = await folderTask;

			return folder is null ? null : new WindowsStorageFolder(folder);
		}

		// WINUI3
		private FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);

			return obj;
		}
	}
}
