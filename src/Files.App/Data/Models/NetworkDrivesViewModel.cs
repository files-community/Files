﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.Core.Services;
using Files.Core.Storage.LocatableStorage;

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

			var networkItem = new SideBarDriveItem
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = SidebarItemType.Drive,
			};
			networkItem.MenuOptions = new SidebarContextMenuOptions
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
			unsortedDrives.Add(drives.Single(x => x is SideBarDriveItem o && o.DeviceID == "network-folder"));
			await foreach (ILocatableFolder item in networkDrivesService.GetDrivesAsync())
			{
				unsortedDrives.Add(item);
			}

			var orderedDrives = unsortedDrives.Cast<SideBarDriveItem>()
				.OrderByDescending(o => o.DeviceID == "network-folder")
				.ThenBy(o => o.Text);

			Drives.Clear();
			foreach (ILocatableFolder item in orderedDrives)
			{
				Drives.AddIfNotPresent(item);
			}
		}

		public void DisconnectNetworkDrive(ILocatableFolder drive)
			=> networkDrivesService.DisconnectNetworkDrive(drive);

		public Task OpenMapNetworkDriveDialogAsync()
			=> networkDrivesService.OpenMapNetworkDriveDialogAsync();
	}
}
