using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared.Extensions;
using Files.Uwp.Storage.WindowsStorage;

#nullable enable

namespace Files.Uwp.ServicesImplementation
{
    /// <inheritdoc cref="IFileExplorerService"/>
    internal sealed class FileExplorerService : IFileExplorerService
    {
        /// <inheritdoc/>
        public async Task OpenAppFolderAsync(CancellationToken cancellationToken = default)
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task OpenInFileExplorerAsync(ILocatableFolder folder, CancellationToken cancellationToken = default)
        {
            await Launcher.LaunchFolderPathAsync(folder.Path).AsTask(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ILocatableFile?> PickSingleFileAsync(IEnumerable<string>? filter, CancellationToken cancellationToken = default)
        {
            var filePicker = new FileOpenPicker();

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

            if (file is null)
                return null;

            return new WindowsStorageFile(file);
        }

        /// <inheritdoc/>
        public async Task<ILocatableFolder?> PickSingleFolderAsync(CancellationToken cancellationToken = default)
        {
            var folderPicker = new FolderPicker();

            folderPicker.FileTypeFilter.Add("*");

            var folderTask = folderPicker.PickSingleFolderAsync().AsTask(cancellationToken);
            var folder = await folderTask;

            if (folder is null)
                return null;

            return new WindowsStorageFolder(folder);
        }
    }
}
