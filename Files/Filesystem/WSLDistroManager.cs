using Files.DataModels.NavigationControlItems;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem
{
    public class WSLDistroManager
    {
        public event EventHandler<List<INavigationControlItem>> RefreshCompleted;
        public event EventHandler RemoveWslSidebarSection;

        public WSLDistroManager()
        {
        }

        public async Task EnumerateDrivesAsync()
        {
            var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
            var wslDistroList = new List<INavigationControlItem>();
            if ((await distroFolder.GetFoldersAsync()).Count != 0)
            {
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

                    if (!wslDistroList.Any(x => x.Path == folder.Path))
                    {
                        wslDistroList.Add(new WslDistroItem()
                        {
                            Text = folder.DisplayName,
                            Path = folder.Path,
                            Logo = logoURI
                        });
                    }
                }
            }
            RefreshCompleted?.Invoke(this, wslDistroList.ToList());
        }

        public async void UpdateWslSectionVisibility()
        {
            if (App.AppSettings.ShowWslSection)
            {
                await EnumerateDrivesAsync();
            }
            else
            {
                RemoveWslSidebarSection?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}