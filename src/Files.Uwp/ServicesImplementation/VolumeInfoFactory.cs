using Files.Backend.Models;
using Files.Backend.Services;
using Files.Shared.Extensions;
using Files.Uwp.Helpers;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Uwp.ServicesImplementation
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
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is null)
            {
                return string.Empty;
            }

            var parameter = new ValueSet
            {
                ["Arguments"] = "VolumeID",
                ["DriveName"] = driveName,
            };

            var (status, response) = await connection.SendMessageForResponseAsync(parameter);
            if (status is AppServiceResponseStatus.Success)
            {
                return response.Get("VolumeID", string.Empty);
            }

            return string.Empty;
        }
    }
}
