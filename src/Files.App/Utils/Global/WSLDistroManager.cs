// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using static Files.App.Constants;

namespace Files.App.Utils
{
	public static class WSLDistroManager
	{
		public static EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private static readonly List<WslDistroItem> distros = [];
		public static IReadOnlyList<WslDistroItem> Distros
		{
			get
			{
				lock (distros)
				{
					return distros.ToList().AsReadOnly();
				}
			}
		}

		public static async Task UpdateDrivesAsync()
		{
			try
			{
				var names = await Server.DriveService.GetWslDistributionsAsync();
				lock (distros)
				{
					distros.Clear();
					distros.AddRange(names.Select(name => new WslDistroItem
					{
						Text = name,
						Path = $@"\\wsl$\{name}",
						Icon = GetLogoUri(name),
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					}));
				}
				DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
			catch (Exception)
			{
				// WSL may be unavailable on this system.
			}
		}

		public static bool TryGetDistro(string path, [NotNullWhen(true)] out WslDistroItem? distro)
		{
			var normalizedPath = PathNormalization.NormalizePath(path);
			distro = Distros.FirstOrDefault(x => normalizedPath.StartsWith(PathNormalization.NormalizePath(x.Path!), StringComparison.OrdinalIgnoreCase));

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
