// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface INetworkService
	{
		/// <summary>
		/// Gets enumerated network computers.
		/// </summary>
		ObservableCollection<IFolder> Computers { get; }

		/// <summary>
		/// Gets enumerated network shortcuts.
		/// </summary>
		ObservableCollection<IFolder> Shortcuts { get; }

		/// <summary>
		/// Enumerates network computers.
		/// </summary>
		/// <returns>A collection of network computers</returns>
		Task<IEnumerable<IFolder>> GetComputersAsync();

		/// <summary>
		/// Enumerates network shortcuts.
		/// </summary>
		/// <returns>A collection of network shortcuts</returns>
		Task<IEnumerable<IFolder>> GetShortcutsAsync();

		/// <summary>
		/// Updates computers to up-to-date.
		/// </summary>
		/// <returns></returns>
		Task UpdateComputersAsync();

		/// <summary>
		/// Updates shortcuts to up-to-date.
		/// </summary>
		/// <returns></returns>
		Task UpdateShortcutsAsync();

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
		bool DisconnectNetworkDrive(IFolder drive);

		/// <summary>
		/// Authenticates the specified network share point.
		/// </summary>
		/// <param name="path">A path to the network share point.</param>
		/// <returns>True If succeeds; otherwise, false.</returns>
		Task<bool> AuthenticateNetworkShare(string path);
	}
}
