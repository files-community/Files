using Files.Backend.Models;
using Files.Backend.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.ServicesImplementation
{
    internal class StorageVolumeInfoFactory : IVolumeInfoFactory
    {
        public async Task<VolumeInfo> BuildVolumeInfo(string driveName)
        {
            Guid volumeGuid = await GetVolumeGuid(driveName);
            return new VolumeInfo(volumeGuid);
        }

        private async Task<Guid> GetVolumeGuid(string driveName)
        {
            var root = await StorageFolder.GetFolderFromPathAsync(driveName);
            var items = await root.GetItemsAsync();
            if (items.Any() && items.First() is IStorageItemProperties item)
            {
                var property = await item.Properties.RetrievePropertiesAsync(new string[] { "System.VolumeId" });
                if (property.TryGetValue("System.VolumeId", out object id) && Guid.TryParse(id.ToString(), out Guid guid))
                {
                    return guid;
                }
            }
            return Guid.Empty;
        }
    }
}
