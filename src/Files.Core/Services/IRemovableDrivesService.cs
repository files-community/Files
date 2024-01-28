// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.


namespace Files.Core.Services
{
	/// <summary>
	/// Represents a service to enumerate drives and create a storage device watcher
	/// </summary>
	public interface IRemovableDrivesService : INotifyPropertyChanged, IDisposable
	{
		/// <summary>
		/// Gets a list of drives.
		/// </summary>
		ObservableCollection<ILocatableFolder> RemovableDrives { get; }

		/// <summary>
		/// Gets the value that indicates whether the app should show confirmation dialog on initialization.
		/// </summary>
		bool ShowUserConsentOnInit { get; set; }

		/// <summary>
		/// Gets the primary system drive. This item is typically excluded when enumerating removable drives
		/// </summary>
		/// <returns>The location of the drive which the operating system is installed to.</returns>
		Task<ILocatableFolder> GetPrimaryDriveAsync();

		/// <summary>
		/// Creates a watcher for storage devices
		/// </summary>
		/// <returns>The created storage device watcher</returns>
		void InitializeRemovableDrivesWatcher();

		/// <summary>
		/// Enumerates all removable drives
		/// </summary>
		/// <returns>A collection of removable storage devices</returns>
		IAsyncEnumerable<ILocatableFolder> GetDrivesAsync();

		/// <summary>
		/// Refreshes the properties of a drive
		/// </summary>
		/// <param name="drive"></param>
		/// <returns></returns>
		Task UpdateDrivePropertiesAsync(ILocatableFolder drive);

		/// <summary>
		/// Refreshes the list of drives.
		/// </summary>
		/// <returns></returns>
		Task RefreshRemovableDrivesAsync();
	}
}
