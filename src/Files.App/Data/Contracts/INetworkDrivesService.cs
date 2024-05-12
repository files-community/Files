// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage.LocatableStorage;

namespace Files.App.Data.Contracts
{
	public interface INetworkDrivesService
	{
		/// <summary>
		/// Gets enumerated network storage drives.
		/// </summary>
		ObservableCollection<ILocatableFolder> Drives { get; }

		/// <summary>
		/// Enumerates network storage drives.
		/// </summary>
		/// <returns>A collection of network storage devices</returns>
		IAsyncEnumerable<ILocatableFolder> GetDrivesAsync();

		/// <summary>
		/// Updates network storage drives to up-to-date.
		/// </summary>
		/// <returns></returns>
		Task UpdateDrivesAsync();

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
		/// Authenticates the specified network share point.
		/// </summary>
		/// <param name="path">A path to the network share point.</param>
		/// <returns>True If succeeds; otherwise, false.</returns>
		Task<bool> AuthenticateNetworkShare(string path);
	}
}
