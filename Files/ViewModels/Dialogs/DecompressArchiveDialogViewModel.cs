using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Files.ViewModels.Dialogs
{
    public class DecompressArchiveDialogViewModel : ObservableObject
    {
        private readonly IStorageFile _archive;

        public StorageFolder DestinationFolder { get; private set; }

        private string _DestinationFolderPath;
        public string DestinationFolderPath
        {
            get => _DestinationFolderPath;
            private set => SetProperty(ref _DestinationFolderPath, value);
        }

        private bool _OpenDestinationFolderOnCompletion;
        public bool OpenDestinationFolderOnCompletion
        {
            get => _OpenDestinationFolderOnCompletion;
            set => SetProperty(ref _OpenDestinationFolderOnCompletion, value);
        }

        public ICommand SelectDestinationCommand { get; private set; }

        public DecompressArchiveDialogViewModel(IStorageFile archive)
        {
            this._archive = archive;
            this.DestinationFolderPath = DefaultDestinationFolderPath();

            // Create commands
            SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
        }

        private async Task SelectDestination()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            DestinationFolder = await folderPicker.PickSingleFolderAsync();

            if (DestinationFolder != null)
            {
                DestinationFolderPath = DestinationFolder.Path;
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
