using Files.Backend.Models;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface IVolumeInfoFactory
	{
		VolumeInfo BuildVolumeInfo(string driveName);
	}
}
