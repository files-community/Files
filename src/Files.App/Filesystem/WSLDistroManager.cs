// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using static Files.App.Constants;

namespace Files.App.Filesystem
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
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
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

		public bool TryGetDistro(string path, out WslDistroItem? distro)
		{
			var normalizedPath = PathNormalization.NormalizePath(path);
			distro = Distros.FirstOrDefault(x => normalizedPath.StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));

			return distro is not null;
		}

		private static Uri GetLogoUri(string displayName)
		{
			if (Contains(displayName, "ubuntu"))
			{
				return new Uri(WslIconsPaths.UbuntuIcon);
			}
			if (Contains(displayName, "kali"))
			{
				return new Uri(WslIconsPaths.KaliIcon);
			}
			if (Contains(displayName, "debian"))
			{
				return new Uri(WslIconsPaths.DebianIcon);
			}
			if (Contains(displayName, "opensuse"))
			{
				return new Uri(WslIconsPaths.OpenSuse);
			}
			return Contains(displayName, "alpine") ? new Uri(WslIconsPaths.Alpine) : new Uri(WslIconsPaths.GenericIcon);

			static bool Contains(string displayName, string distroName)
				=> displayName.Contains(distroName, StringComparison.OrdinalIgnoreCase);
		}
	}
}