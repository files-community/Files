using Files.Backend.Models;

namespace Files.Backend.Services
{
	public interface IVolumeInfoFactory
	{
		VolumeInfo BuildVolumeInfo(string driveName);
	}
}
