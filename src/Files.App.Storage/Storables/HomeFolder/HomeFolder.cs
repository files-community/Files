// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Win32;

namespace Files.App.Storage.Storables
{
	public partial class HomeFolder : IHomeFolder
	{
		public string Id => "Home"; // Will be "files://Home" in the future.

		public string Name => "Home";

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.Folder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var folder in GetQuickAccessFolderAsync(cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();

				yield return folder;
			}

			await foreach (var drive in GetLogicalDrivesAsync(cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();

				yield return drive;
			}

			await foreach (var location in GetNetworkLocationsAsync(cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();

				yield return location;
			}
		}

		/// <inheritdoc/>
		public IAsyncEnumerable<IStorableChild> GetQuickAccessFolderAsync(CancellationToken cancellationToken = default)
		{
			IFolder folder = new WindowsFolder(new Guid("3936e9e4-d92c-4eee-a85a-bc16d5ea0819"));
			return folder.GetItemsAsync(StorableType.Folder, cancellationToken);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorableChild> GetLogicalDrivesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var availableDrives = PInvoke.GetLogicalDrives();
			if (availableDrives is 0)
				yield break;

			int count = BitOperations.PopCount(availableDrives);
			var driveLetters = new char[count];

			count = 0;
			char driveLetter = 'A';
			while (availableDrives is not 0)
			{
				if ((availableDrives & 1) is not 0)
					driveLetters[count++] = driveLetter;

				availableDrives >>= 1;
				driveLetter++;
			}

			foreach (int letter in driveLetters)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (WindowsStorable.TryParse($"{letter}:\\") is not IWindowsStorable driveRoot)
					throw new InvalidOperationException();

				yield return new WindowsFolder(driveRoot.ThisPtr);
				await Task.Yield();
			}

		}

		/// <inheritdoc/>
		public IAsyncEnumerable<IStorableChild> GetNetworkLocationsAsync(CancellationToken cancellationToken = default)
		{
			Guid FOLDERID_NetHood = new("{C5ABBF53-E17F-4121-8900-86626FC2C973}");
			IFolder folder = new WindowsFolder(FOLDERID_NetHood);
			return folder.GetItemsAsync(StorableType.Folder, cancellationToken);
		}

		/// <inheritdoc/>
		public IAsyncEnumerable<IStorableChild> GetRecentFilesAsync(CancellationToken cancellationToken = default)
		{
			Guid FOLDERID_NetHood = new("{AE50C081-EBD2-438A-8655-8A092E34987A}");
			IFolder folder = new WindowsFolder(FOLDERID_NetHood);
			return folder.GetItemsAsync(StorableType.Folder, cancellationToken);
		}
	}
}
