using Files.App.Helpers;
using Files.Core.Models;
using Files.Core.Services;

namespace Files.App.ServicesImplementation
{
	internal class VolumeInfoFactory : IVolumeInfoFactory
	{
		public VolumeInfo BuildVolumeInfo(string driveName)
		{
			string volumeId = GetVolumeID(driveName);

			return new VolumeInfo(volumeId);
		}

		private string GetVolumeID(string driveName)
		{
			return DriveHelpers.GetVolumeId(driveName);
		}
	}
}
