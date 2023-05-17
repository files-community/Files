// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.LocatableStorage;

namespace Files.App.Data.Models
{
	public class NetworkDrivesModel : ObservableObject
	{
		private readonly INetworkDrivesService _networkDrivesService;

		private ObservableCollection<ILocatableFolder> _Drives;
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _Drives;
			private set => SetProperty(ref _Drives, value);
		}

		public NetworkDrivesModel(INetworkDrivesService networkDrivesService)
		{
			_networkDrivesService = networkDrivesService;

			_Drives = new();

			var networkItem = new DriveItem()
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
			};

			networkItem.MenuOptions = new()
			{
				IsLocationItem = true,
				ShowShellItems = true,
				ShowEjectDevice = networkItem.IsRemovable,
				ShowProperties = true
			};

			lock (_Drives)
			{
				_Drives.Add(networkItem);
			}
		}

		public async Task UpdateDrivesAsync()
		{
			var unsortedDrives = new List<ILocatableFolder>();
			Drives.Clear();

			await foreach (ILocatableFolder item in _networkDrivesService.GetDrivesAsync())
				unsortedDrives.Add(item);

			var orderedDrives = unsortedDrives
				.Cast<DriveItem>()
				.OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				.ThenBy(o => o.Text);

			foreach (ILocatableFolder item in orderedDrives)
				Drives.AddIfNotPresent(item);
		}

		public void DisconnectNetworkDrive(ILocatableFolder drive)
		{
			_networkDrivesService.DisconnectNetworkDrive(drive);
		}

		public Task OpenMapNetworkDriveDialogAsync()
		{
			return _networkDrivesService.OpenMapNetworkDriveDialogAsync();
		}
	}
}
