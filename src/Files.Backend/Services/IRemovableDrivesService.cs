using Files.Backend.Models;
using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface IRemovableDrivesService
	{
		/// <summary>
		/// Gets the primary system drive. This item is typically excluded when enumerating removable drives.
		/// </summary>
		/// <returns>The location of the drive which the operating system is installed to.</returns>
		Task<string> GetPrimaryDrivePathAsync();

		IStorageDeviceWatcher CreateWatcher();

		Task<IReadOnlyList<ILocatableFolder>> GetDrivesAsync();

		Task UpdateDrivePropertiesAsync(ILocatableFolder drive);
	}
}
