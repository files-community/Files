using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface INetworkDrivesService
	{
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
	}
}
