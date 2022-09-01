using Files.Backend.Models;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IVolumeInfoFactory
    {
        Task<VolumeInfo> BuildVolumeInfo(string driveName);
    }
}
