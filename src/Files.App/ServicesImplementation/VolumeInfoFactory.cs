using Files.Backend.Models;
using Files.Backend.Services;
using Files.App.Helpers;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
    internal class VolumeInfoFactory : IVolumeInfoFactory
    {
        public async Task<VolumeInfo> BuildVolumeInfo(string driveName)
        {
            string volumeId = await GetVolumeID(driveName);
            return new VolumeInfo(volumeId);
        }

        private async Task<string> GetVolumeID(string driveName)
        {
            return DriveHelpers.GetVolumeId(driveName);
        }
    }
}
