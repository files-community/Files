using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
	class Drives
	{
		public Task<ObservableCollection<DriveItem>> LocalDrives { get; }

		public Task<ObservableCollection<DriveItem>> RemovableDrives { get; }

		public Task<ObservableCollection<DriveItem>> VirtualDrives { get; }

		public Task<ObservableCollection<DriveItem>> AllDrives { get; }

		Drives()
		{
			LocalDrives = GetLocalDrivesListAsync();
			RemovableDrives = GetRemovableDrivesListAsync();
			VirtualDrives = GetVirtualDrivesListAsync();

			InitAllDrivesList();
		}

		private async void InitAllDrivesList()
		{
			var localDrives = await LocalDrives;
			localDrives.CollectionChanged += (sender, args) => { };
		}

		private Task<ObservableCollection<DriveItem>> GetLocalDrivesListAsync()
		{
			return Task.Run(() =>
			{
				var drives = new ObservableCollection<DriveItem>();

				var driveLetters = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).OrderBy(x => x.Root.FullName)
					.ToList();
				var removableDevicesTask = KnownFolders.RemovableDevices.GetFoldersAsync().AsTask();
				removableDevicesTask.Wait();
				var removableDevicesPaths = removableDevicesTask.Result.Select(x => x.Path).ToList();


				foreach (var driveLetter in driveLetters)
				{
					try
					{
						var content = string.Empty;
						string icon = null;

						if (!removableDevicesPaths.Contains(driveLetter.Name))
						{
							// TODO: Display Custom Names for Local Disks as well
							if (InstanceTabsView.NormalizePath(driveLetter.Name) != InstanceTabsView.NormalizePath("A:")
								&& InstanceTabsView.NormalizePath(driveLetter.Name) !=
								InstanceTabsView.NormalizePath("B:"))
							{
								content = $"Local Disk ({driveLetter.Name.TrimEnd('\\')})";
								icon = "\uEDA2";
							}
							else
							{
								content = $"Floppy Disk ({driveLetter.Name.TrimEnd('\\')})";
								icon = "\uE74E";
							}

							Visibility capacityBarVis = Visibility.Visible;
							ulong totalSpaceProg = 0;
							ulong freeSpaceProg = 0;
							string free_space_text = "Unknown";
							string total_space_text = "Unknown";

							try
							{

								var driveTask = StorageFolder.GetFolderFromPathAsync(driveLetter.Name).AsTask();
								driveTask.Wait();
								var retrivedPropertiesTask = driveTask.Result.Properties
									.RetrievePropertiesAsync(new string[] { "System.FreeSpace", "System.Capacity" })
									.AsTask();
								var retrivedProperties = retrivedPropertiesTask.Result;

								var sizeAsGBString = ByteSizeLib.ByteSize
									.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).GigaBytes;
								freeSpaceProg = Convert.ToUInt64(sizeAsGBString);

								sizeAsGBString = ByteSizeLib.ByteSize
									.FromBytes((ulong)retrivedProperties["System.Capacity"]).GigaBytes;
								totalSpaceProg = Convert.ToUInt64(sizeAsGBString);

								free_space_text = ByteSizeLib.ByteSize
									.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).ToString();
								total_space_text = ByteSizeLib.ByteSize
									.FromBytes((ulong)retrivedProperties["System.Capacity"]).ToString();
							}
							catch (UnauthorizedAccessException)
							{
								capacityBarVis = Visibility.Collapsed;
							}
							catch (NullReferenceException)
							{
								capacityBarVis = Visibility.Collapsed;
							}

							drives.Add(new DriveItem()
							{
								driveText = content,
								glyph = icon,
								maxSpace = totalSpaceProg,
								spaceUsed = totalSpaceProg - freeSpaceProg,
								tag = driveLetter.Name,
								progressBarVisibility = capacityBarVis,
								spaceText = free_space_text + " free of " + total_space_text,
							});
						}
					}
					catch (UnauthorizedAccessException e)
					{
						Debug.WriteLine(e.Message);
					}
				}

				return drives;
			});
		}

		private Task<ObservableCollection<DriveItem>> GetRemovableDrivesListAsync()
		{
			return Task.Run(() =>
			{
				var drives = new ObservableCollection<DriveItem>();

				// TODO Get RemovableDrives

				return drives;
			});
		}

		private Task<ObservableCollection<DriveItem>> GetVirtualDrivesListAsync()
		{
			return Task.Run(() =>
			{
				var drives = new ObservableCollection<DriveItem>();

				var oneDriveItem = new DriveItem()
				{
					driveText = "OneDrive",
					tag = "OneDrive",
					cloudGlyphVisibility = Visibility.Visible,
					driveGlyphVisibility = Visibility.Collapsed
				};

				drives.Add(oneDriveItem);

				return drives;
			});
		}
	}
}
