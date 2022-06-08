using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using Files.Uwp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels.Widgets.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        #region Actions

        public Action<string, FilesystemItemType, bool, bool, IEnumerable<string>> OpenPath { get; set; }

        public Action<string> OpenPathInNewPane { get; set; }

        public Action<BundleItemViewModel> NotifyItemRemoved { get; set; }

        #endregion Actions

        #region Properties

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IBundlesSettingsService BundlesSettingsService { get; } = Ioc.Default.GetService<IBundlesSettingsService>();

        /// <summary>
        /// The name of a bundle this item is contained within
        /// </summary>
        public string ParentBundleName { get; set; }

        public string Path { get; set; }

        public string Name
        {
            get
            {
                string fileName;

                // Network Share path
                if (System.IO.Path.GetPathRoot(this.Path) == this.Path && this.Path.StartsWith(@"\\")) 
                {
                    fileName = this.Path.Substring(this.Path.LastIndexOf(@"\") + 1);
                }
                // Drive path
                else if (System.IO.Path.GetPathRoot(this.Path) == this.Path)
                {
                    fileName = this.Path;
                }
                else
                {
                    fileName = System.IO.Path.GetFileName(this.Path);
                }


                if (fileName.EndsWith(".lnk", StringComparison.Ordinal) || fileName.EndsWith(".url", StringComparison.Ordinal))
                {
                    fileName = fileName.Remove(fileName.Length - 4);
                }

                return fileName;
            }
        }

        public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

        private ImageSource icon;

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public SvgImageSource FolderIcon { get; } = new SvgImageSource()
        {
            RasterizePixelHeight = 128,
            RasterizePixelWidth = 128,
            UriSource = new Uri("ms-appx:///Assets/FolderIcon.svg"),
        };

        public bool OpenInNewTabLoad
        {
            get => TargetType == FilesystemItemType.Directory;
        }

        public bool OpenInNewPaneLoad
        {
            get => UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled && TargetType == FilesystemItemType.Directory;
        }

        #endregion Properties

        #region Commands

        public ICommand OpenInNewTabCommand { get; private set; }

        public ICommand OpenInNewPaneCommand { get; private set; }

        public ICommand OpenItemLocationCommand { get; private set; }

        public ICommand RemoveItemCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public BundleItemViewModel(string path, FilesystemItemType targetType)
        {
            this.Path = path;
            this.TargetType = targetType;

            // Create commands
            OpenInNewTabCommand = new RelayCommand(OpenInNewTab);
            OpenInNewPaneCommand = new RelayCommand(OpenInNewPane);
            OpenItemLocationCommand = new RelayCommand(OpenItemLocation);
            RemoveItemCommand = new RelayCommand(RemoveItem);
        }

        #endregion Constructor

        #region Command Implementation

        private async void OpenInNewTab()
        {
            await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), Path);
        }

        private void OpenInNewPane()
        {
            OpenPathInNewPane(Path);
        }

        private void OpenItemLocation()
        {
            OpenPath(System.IO.Path.GetDirectoryName(Path), FilesystemItemType.Directory, false, false, System.IO.Path.GetFileName(Path).CreateEnumerable());
        }

        #endregion Command Implementation

        #region Public Helpers

        public async Task UpdateIcon()
        {
            if (TargetType == FilesystemItemType.Directory) // OpenDirectory
            {
                Icon = FolderIcon;
            }
            else // NotADirectory
            {
                var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(Path, 24u, ThumbnailMode.ListView);
                if (iconData != null)
                {
                    Icon = await iconData.ToBitmapAsync();
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }

        public void OpenItem()
        {
            OpenPath(Path, TargetType, false, false, null);
        }

        public void RemoveItem()
        {
            if (BundlesSettingsService.SavedBundles.ContainsKey(ParentBundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettingsService.SavedBundles;
                allBundles[ParentBundleName].Remove(Path);
                BundlesSettingsService.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        #endregion Public Helpers

        #region IDisposable

        public void Dispose()
        {
            Icon = null;

            OpenPath = null;
            OpenPathInNewPane = null;
        }

        #endregion IDisposable
    }
}