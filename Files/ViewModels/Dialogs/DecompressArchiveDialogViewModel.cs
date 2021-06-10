using Files.Enums;
using Files.Helpers;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace Files.ViewModels.Dialogs
{
    public class DecompressArchiveDialogViewModel : ObservableObject
    {
        private StorageFile _archive;

        private StorageFolder _destinationFolder;

        public string DestinationFolderLocation
        {
            get => _destinationFolder?.Path;
        }

        public ICommand StartExtractingCommand { get; private set; }

        public DecompressArchiveDialogViewModel(StorageFile archive, StorageFolder destinationFolder)
        {
            this._archive = archive;
            this._destinationFolder = destinationFolder;

            // Create commands
            StartExtractingCommand = new RelayCommand(StartExtracting);
        }

        private async void StartExtracting()
        {
            // Check if archive still exists
            if (!StorageItemHelpers.Exists(_archive.Path))
            {
                return;
            }

            PostedStatusBanner banner = App.StatusCenterViewModel.PostOperationBanner(
                string.Empty,
                "Extracting archive",
                0,
                ReturnResult.InProgress,
                FileOperationType.Extract,
                new CancellationTokenSource());

            StorageFolder childDestinationFolder = await _destinationFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(_archive.Path), CreationCollisionOption.OpenIfExists);
            await ZipHelpers.ExtractArchive(_archive, childDestinationFolder, banner.Progress);
        }
    }
}
