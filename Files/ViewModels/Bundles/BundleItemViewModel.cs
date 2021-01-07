using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml.Media.Imaging;
using Files.Filesystem;

namespace Files.ViewModels.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        #region Private Members

        private readonly IShellPage associatedInstance;

        #endregion

        #region Public Properties

        public string Path { get; set; }

        public string Name
        {
            get => System.IO.Path.GetFileNameWithoutExtension(this.Path);
        }

        public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

        public BitmapImage Icon
        {
            get
            {
                if (TargetType == FilesystemItemType.Directory) // OpenDirectory
                {
                    return (BitmapImage)null;
                }
                else // NotADirectory
                {
                    return Task.Run(async () => await associatedInstance.FilesystemViewModel.LoadIconOverlayAsync(Path, 80u)).GetAwaiter().GetResult().Icon;
                }
            }
        }

        #endregion

        #region Commands

        public ICommand ClickCommand { get; set; }

        #endregion

        #region Constructor

        public BundleItemViewModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;

            // Create commands
            ClickCommand = new RelayCommand(async () => await Click());
        }

        #endregion

        #region Command Implementation

        private async Task Click()
        {
            await associatedInstance.InteractionOperations.OpenPath(Path, TargetType);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Path = null;
        }

        #endregion
    }
}
