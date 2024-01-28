// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface INetworkDrivesService : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets a list of drives.
		/// </summary>
		ObservableCollection<ILocatableFolder> NetworkDrives { get; }

		/// <summary>
		/// Enumerates network storage devices
		/// </summary>
		/// <returns>A collection of network storage devices</returns>
		IAsyncEnumerable<ILocatableFolder> GetDrivesAsync();

		/// <summary>
		/// Displays the operating system dialog for connecting to a network storage device
		/// </summary>
		/// <returns></returns>
		Task OpenMapNetworkDriveDialogAsync();

		/// <summary>
		/// Disconnects an existing network storage device
		/// </summary>
		/// <param name="drive">An item representing the network storage device to disconnect from</param>
		/// <returns>True or false to indicate status</returns>
		bool DisconnectNetworkDrive(ILocatableFolder drive);

		/// <summary>
		/// Authenticates network share.
		/// </summary>
		/// <param name="path">The path to share</param>
		/// <returns>True if succeed; otherwise, false.</returns>
		Task<bool> AuthenticateNetworkShare(string path);

		/// <summary>
		/// Refreshes the list of network drives.
		/// </summary>
		/// <returns></returns>
		Task RefreshNetworkDrivesAsync();
	}
}
