using Files.Backend.Models;
using Files.Backend.Services;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation
{
    internal class VolumeInfoFactory : IVolumeInfoFactory
    {
        private readonly IVolumeInfoFactory storageFactory = new StorageVolumeInfoFactory();
        private readonly IVolumeInfoFactory fullTrustFactory = new FullTrustVolumeInfoFactory();

        public async Task<VolumeInfo> BuildVolumeInfo(string driveName)
        {
            VolumeInfo info = await storageFactory.BuildVolumeInfo(driveName);
            if (!info.IsEmpty)
            {
                return info;
            }
            return await fullTrustFactory.BuildVolumeInfo(driveName);
        }
    }
}
