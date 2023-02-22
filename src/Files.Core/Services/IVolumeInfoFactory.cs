using Files.Core.Models;

namespace Files.Core.Services
{
	public interface IVolumeInfoFactory
	{
		VolumeInfo BuildVolumeInfo(string driveName);
	}
}
