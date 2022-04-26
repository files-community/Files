using Files.Uwp.DataModels.NavigationControlItems;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class WSLDistroManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private readonly List<WslDistroItem> distroList = new List<WslDistroItem>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        public IReadOnlyList<WslDistroItem> Distros
        {
            get
            {
                lock (distroList)
                {
                    return distroList.ToList().AsReadOnly();
                }
            }
        }

        public async Task EnumerateDrivesAsync()
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowWslSection)
            {
                return;
            }

            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                {
                    Uri logoURI = null;
                    if (folder.DisplayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
                    }
                    else if (folder.DisplayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/kalipng.png");
                    }
                    else if (folder.DisplayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/debianpng.png");
                    }
                    else if (folder.DisplayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/opensusepng.png");
                    }
                    else if (folder.DisplayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/alpinepng.png");
                    }
                    else
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/genericpng.png");
                    }

                    if (!distroList.Any(x => x.Path == folder.Path))
                    {
                        var distro = new WslDistroItem()
                        {
                            Text = folder.DisplayName,
                            Path = folder.Path,
                            Logo = logoURI,
                            MenuOptions = new ContextMenuOptions
                            {
                                IsLocationItem = true
                            }
                        };
                        distroList.Add(distro);
                        DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, distro));
                    }
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
            }
        }
    }
}