// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;
using Windows.Storage;

namespace Files.App.Filesystem
{
	/// <summary>
	/// Provides handler for WSL Distributions.
	/// </summary>
	public class WSLDistroManager
	{
		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<WslDistroItem> _Distros = new();
		public IReadOnlyList<WslDistroItem> Distros
		{
			get
			{
				lock (_Distros)
					return _Distros.ToList().AsReadOnly();
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

					lock (_Distros)
					{
						if (_Distros.Any(x => x.Path == folder.Path))
							continue;

						_Distros.Add(distro);
					}

					DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, distro));
				}
			}
			catch (Exception)
			{
				// WSL is not supported or enabled
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
				return new Uri(Constants.WslIconsPaths.UbuntuIcon);

			if (Contains(displayName, "kali"))
				return new Uri(Constants.WslIconsPaths.KaliIcon);

			if (Contains(displayName, "debian"))
				return new Uri(Constants.WslIconsPaths.DebianIcon);

			if (Contains(displayName, "opensuse"))
				return new Uri(Constants.WslIconsPaths.OpenSuse);

			return Contains(displayName, "alpine") ? new Uri(Constants.WslIconsPaths.Alpine) : new Uri(Constants.WslIconsPaths.GenericIcon);

			static bool Contains(string displayName, string distroName)
				=> displayName.Contains(distroName, StringComparison.OrdinalIgnoreCase);
		}
	}
}
