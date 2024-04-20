// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Windows.Storage;

namespace Files.App.Services
{
	/// <inheritdoc　cref="IWSLDrivesService"/>
	internal class WSLDrivesService : IWSLDrivesService
	{
		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly List<WslDistroItem> _WSLDrives = [];
		/// <inheritdoc/>
		public IReadOnlyList<WslDistroItem> WSLDrives
		{
			get
			{
				lock (_WSLDrives)
					return _WSLDrives.ToList().AsReadOnly();
			}
		}

		/// <inheritdoc/>
		public async Task UpdateDrivesAsync()
		{
			try
			{
				var wslFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");

				foreach (var folder in await wslFolder.GetFoldersAsync())
				{
					var item = new WslDistroItem()
					{
						Text = folder.DisplayName,
						Path = folder.Path,
						Icon = GetLogoUri(folder.DisplayName),
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (_WSLDrives)
					{
						if (_WSLDrives.Any(x => x.Path == folder.Path))
							continue;

						_WSLDrives.Add(item);
					}

					DataChanged?.Invoke(SectionType.WSL, new(NotifyCollectionChangedAction.Add, item));
				}
			}
			catch (Exception)
			{
			}
		}

		/// <inheritdoc/>
		public bool TryGet(string path, [NotNullWhen(true)] out WslDistroItem? item)
		{
			var normalizedPath = PathNormalization.NormalizePath(path);

			item = WSLDrives.FirstOrDefault(x =>
				normalizedPath.StartsWith(
					PathNormalization.NormalizePath(x.Path),
					StringComparison.OrdinalIgnoreCase));

			return item is not null;
		}

		private static Uri GetLogoUri(string displayName)
		{
			if (displayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
				return new Uri(Constants.WslIconsPaths.UbuntuIcon);
			else if (displayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
				return new Uri(Constants.WslIconsPaths.KaliIcon);
			else if (displayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
				return new Uri(Constants.WslIconsPaths.DebianIcon);
			else if (displayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
				return new Uri(Constants.WslIconsPaths.OpenSuse);
			else if (displayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
				return new Uri(Constants.WslIconsPaths.Alpine);
			else
				return new Uri(Constants.WslIconsPaths.GenericIcon);
		}
	}
}
