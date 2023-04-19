using Files.Backend.Models;
using Files.Backend.Services;

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
