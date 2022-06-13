using Files.Uwp.DataModels.NavigationControlItems;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.Filesystem
{
    public class WSLDistroManager
    {
        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        private readonly List<WslDistroItem> distros = new();
        public IReadOnlyList<WslDistroItem> Distros
        {
            get
            {
                lock (distros)
                {
                    return distros.ToList().AsReadOnly();
                }
            }
        }

        public async Task UpdateDrivesAsync()
        {
            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                {
                    Uri logoURI = GetLogoUri(folder.DisplayName);

                    var distro = new WslDistroItem
                    {
                        Text = folder.DisplayName,
                        Path = folder.Path,
                        Logo = logoURI,
                        MenuOptions = new ContextMenuOptions{ IsLocationItem = true },
                    };

                    lock (distros)
                    {
                        if (distros.Any(x => x.Path == folder.Path))
                        {
                            continue;
                        }
                        distros.Add(distro);
                    }
                    DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, distro));
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
            }
        }

        private static Uri GetLogoUri(string displayName)
        {
            if (Contains(displayName, "ubuntu"))
            {
                return new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
            }
            if (Contains(displayName, "kali"))
            {
                return new Uri("ms-appx:///Assets/WSL/kalipng.png");
            }
            if (Contains(displayName, "debian"))
            {
                return new Uri("ms-appx:///Assets/WSL/debianpng.png");
            }
            if (Contains(displayName, "opensuse"))
            {
                return new Uri("ms-appx:///Assets/WSL/opensusepng.png");
            }
            if (Contains(displayName, "alpine"))
            {
                return new Uri("ms-appx:///Assets/WSL/alpinepng.png");
            }
            return new Uri("ms-appx:///Assets/WSL/genericpng.png");

            static bool Contains(string displayName, string distroName)
                => displayName.Contains(distroName, StringComparison.OrdinalIgnoreCase);
        }
    }
}