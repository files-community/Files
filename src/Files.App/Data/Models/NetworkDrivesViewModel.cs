// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
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
		private readonly INetworkDrivesService networkDrivesService;

		public NetworkDrivesViewModel(INetworkDrivesService networkDrivesService)
		{
			this.networkDrivesService = networkDrivesService;
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

			await foreach (ILocatableFolder item in networkDrivesService.GetDrivesAsync())
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

		public void DisconnectNetworkDrive(ILocatableFolder drive) => networkDrivesService.DisconnectNetworkDrive(drive);
		public Task OpenMapNetworkDriveDialogAsync() => networkDrivesService.OpenMapNetworkDriveDialogAsync();
	}
}