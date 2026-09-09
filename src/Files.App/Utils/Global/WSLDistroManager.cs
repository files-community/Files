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

		public static Task UpdateDrivesAsync()
		{
			// Enumerate from the registry; opening \\wsl$\<distro> via the shell would start that distro's VM.
			return Task.Run(() =>
			{
				try
				{
					const string LxssRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";
					using var lxssKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(LxssRegistryPath);
					if (lxssKey is null)
						return;

					foreach (var subKeyName in lxssKey.GetSubKeyNames())
					{
						using var distroKey = lxssKey.OpenSubKey(subKeyName);
						if (distroKey?.GetValue("DistributionName") is not string distroName || string.IsNullOrEmpty(distroName))
							continue;

						var path = $@"\\wsl$\{distroName}";

						var distro = new WslDistroItem
						{
							Text = distroName,
							Path = path,
							Icon = GetLogoUri(distroName),
							MenuOptions = new ContextMenuOptions { IsLocationItem = true },
						};

						lock (distros)
						{
							if (distros.Any(x => x.Path == path))
								continue;
							distros.Add(distro);
						}
						DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, distro));
					}
				}
				catch (Exception)
				{
					// WSL not supported/enabled
				}
			});
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
