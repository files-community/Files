// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
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
