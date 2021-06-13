using Files.Enums;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Files.ViewModels.Dialogs
{
    public class DecompressArchiveDialogViewModel : ObservableObject
    {
        private StorageFile _archive;

        private StorageFolder _destinationFolder;

        public string ArchiveName
        {
            get => Path.GetFileName(_archive.Path);
        }

        private string _DestinationFolderPath;
        public string DestinationFolderPath
        {
            get => _DestinationFolderPath;
            set => SetProperty(ref _DestinationFolderPath, value);
        }

        public ICommand StartExtractingCommand { get; private set; }

        public ICommand SelectDestinationCommand { get; private set; }

        public DecompressArchiveDialogViewModel(StorageFile archive)
        {
            this._archive = archive;
            this.DestinationFolderPath = DefaultDestinationFolderPath();

            // Create commands
            StartExtractingCommand = new AsyncRelayCommand(StartExtracting);
            SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
        }

        private async Task StartExtracting()
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

            if (_destinationFolder == null)
            {
                StorageFolder parentFolder = await StorageItemHelpers.ToStorageItem<StorageFolder>(Path.GetDirectoryName(_archive.Path));
                _destinationFolder = await parentFolder.CreateFolderAsync(Path.GetFileName(DestinationFolderPath));
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            await ZipHelpers.ExtractArchive(_archive, _destinationFolder, banner.Progress);

            sw.Stop();
            banner.Remove();

            if (sw.Elapsed.TotalSeconds >= 6)
            {
                App.StatusCenterViewModel.PostBanner(
                    "Extracting complete!",
                    "The archive extracting completed successfully.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Extract);
            }
        }

        private async Task SelectDestination()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            _destinationFolder = await folderPicker.PickSingleFolderAsync();

            if (_destinationFolder != null)
            {
                DestinationFolderPath = _destinationFolder.Path;
            }
            else
            {
                DestinationFolderPath = DefaultDestinationFolderPath();
            }
        }

        private string DefaultDestinationFolderPath()
        {
            return Path.Combine(Path.GetDirectoryName(_archive.Path), Path.GetFileNameWithoutExtension(_archive.Path));
        }
    }
}
