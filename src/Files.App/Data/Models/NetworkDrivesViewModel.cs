// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;

namespace Files.App.Data.Models
{
	public class NetworkDrivesViewModel : ObservableObject
	{
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => drives;
			private set => SetProperty(ref drives, value);
		}

		private ObservableCollection<ILocatableFolder> drives;

		private readonly INetworkDrivesService _networkDrivesService;

		public NetworkDrivesViewModel(INetworkDrivesService networkDrivesService)
		{
			_networkDrivesService = networkDrivesService;

			drives = new ObservableCollection<ILocatableFolder>();

			var networkItem = new DriveItem
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
			};

			networkItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowShellItems = true,
				ShowEjectDevice = networkItem.IsRemovable,
				ShowProperties = true
			};

			lock (drives)
			{
				drives.Add(networkItem);
			}
		}

		public async Task UpdateDrivesAsync()
		{
			var unsortedDrives = new List<ILocatableFolder>();
			Drives.Clear();

			await foreach (ILocatableFolder item in _networkDrivesService.GetDrivesAsync())
			{
				unsortedDrives.Add(item);
			}

			var orderedDrives = unsortedDrives.Cast<DriveItem>()
				.OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				.ThenBy(o => o.Text);

			foreach (ILocatableFolder item in orderedDrives)
			{
				Drives.AddIfNotPresent(item);
			}
		}

		public void DisconnectNetworkDrive(ILocatableFolder drive)
			=> _networkDrivesService.DisconnectNetworkDrive(drive);

		public Task OpenMapNetworkDriveDialogAsync()
			=> _networkDrivesService.OpenMapNetworkDriveDialogAsync();
	}
}
