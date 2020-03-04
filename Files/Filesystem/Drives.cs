using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts.DataProvider;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
	public class DrivesManager
	{
		public ObservableCollection<DriveItem> Drives { get; } = new ObservableCollection<DriveItem>();
		public bool ShowUserConsentOnInit { get; set; } = false;
		private DeviceWatcher _deviceWatcher;

		public DrivesManager()
		{
			Task findDrivesTask = null;
			try
			{
				findDrivesTask = GetDrives(Drives);
			}
			catch (AggregateException e)
			{
				ShowUserConsentOnInit = true;
			}

			findDrivesTask.ContinueWith((x) => 
			{
				GetVirtualDrivesList(Drives);

				_deviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
				_deviceWatcher.Added += DeviceAdded;
				_deviceWatcher.Removed += DeviceRemoved;
				_deviceWatcher.Updated += DeviceUpdated;
				_deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
				_deviceWatcher.Start();
			});
			
		}

		private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
		{
			if(App.sideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "Drives") == null)
			{
				App.sideBarItems.Add(new HeaderTextItem() { Text = "Drives" });
			}
			foreach (DriveItem drive in Drives)
			{
				if (!App.sideBarItems.Contains(drive))
				{
					App.sideBarItems.Add(drive);
				}
			}

			foreach(INavigationControlItem item in App.sideBarItems.ToList())
			{
				if(item is DriveItem && !Drives.Contains(item))
				{
					App.sideBarItems.Remove(item);
				}
			}
		}

		private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
		{
			var deviceId = args.Id;

			var root = StorageDevice.FromId(deviceId);

			// If drive already in list, skip.
			if (Drives.Any(x => x.tag == root.Name))
			{
				return;
			}

			DriveType type = DriveType.Removable;

			var driveItem = new DriveItem(
				root,
				Visibility.Visible,
				type);

			// Update the collection on the ui-thread.
			try
			{
				CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => 
				{
					Drives.Add(driveItem);
					DeviceWatcher_EnumerationCompleted(null, null);
				});
			}
			catch (Exception e)
			{
				// Ui-Thread not yet created.
				Drives.Add(driveItem);
			}
		}

		private async void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			var drives = DriveInfo.GetDrives().Select(x => x.Name);

			foreach (var drive in Drives)
			{
				if (drive.Type == DriveType.VirtualDrive || drives.Contains(drive.tag) )
				{
					continue;
				}

				// Update the collection on the ui-thread.
				try
				{
					CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => 
					{ 
						Drives.Remove(drive);
						DeviceWatcher_EnumerationCompleted(null, null);
					});
				}
				catch (Exception e)
				{
					// Ui-Thread not yet created.
					Drives.Remove(drive);
				}
				return;
			}
		}

		private void DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			Debug.WriteLine("Devices updated");
		}

		private async Task GetDrives(IList<DriveItem> list)
		{
			var drives = DriveInfo.GetDrives().ToList();

			var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
			var supportedDevicesNames = remDevices.Select(x => StorageDevice.FromId(x.Id).Name);
			foreach (DriveInfo driveInfo in drives.ToList())
			{
				if (!supportedDevicesNames.Contains(driveInfo.Name) && driveInfo.DriveType == System.IO.DriveType.Removable)
				{
					drives.Remove(driveInfo);
				}
			}


			foreach (var drive in drives)
			{
				// If drive already in list, skip.
				if (list.Any(x => x.tag == drive.Name))
				{
					continue;
				}

				var folder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(drive.Name)).Result;

				
				DriveType type = DriveType.Unkown;

				switch (drive.DriveType)
				{
					case System.IO.DriveType.CDRom:
						type = DriveType.CDRom;
						break;
					case System.IO.DriveType.Fixed:
						if(InstanceTabsView.NormalizePath(drive.Name) != InstanceTabsView.NormalizePath("A:")
						    && InstanceTabsView.NormalizePath(drive.Name) !=
						    InstanceTabsView.NormalizePath("B:"))
						{
							type = DriveType.Fixed;
						}
						else
						{
							type = DriveType.FloppyDisk;
						}
						break;
					case System.IO.DriveType.Network:
						type = DriveType.Network;
						break;
					case System.IO.DriveType.NoRootDirectory:
						type = DriveType.NoRootDirectory;
						break;
					case System.IO.DriveType.Ram:
						type = DriveType.Ram;
						break;
					case System.IO.DriveType.Removable:
						type = DriveType.Removable;

						break;
					case System.IO.DriveType.Unknown:
						type = DriveType.Unkown;
						break;
					default:
						type = DriveType.Unkown;
						break;
				}

				var driveItem = new DriveItem(
					folder,
					Visibility.Visible,
					type);

				list.Add(driveItem);
			}
		}

		private void GetVirtualDrivesList(IList<DriveItem> list)
		{
			var oneDriveItem = new DriveItem()
			{
				driveText = "OneDrive",
				tag = "OneDrive",
				cloudGlyphVisibility = Visibility.Visible,
				driveGlyphVisibility = Visibility.Collapsed,
				Type = DriveType.VirtualDrive,
				//itemVisibility = App.AppSettings.PinOneDriveToSideBar
			};

			var setting = ApplicationData.Current.LocalSettings.Values["PinOneDrive"];
			if (setting == null || (bool)setting == true)
			{
				list.Add(oneDriveItem);
			}
		}

		public void Dispose()
		{
			if (_deviceWatcher.Status == DeviceWatcherStatus.Started || _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
			{
				_deviceWatcher.Stop();
			}
		}
	}
}
