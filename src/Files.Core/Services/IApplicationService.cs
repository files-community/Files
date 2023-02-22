using System.Threading.Tasks;

namespace Files.Core.Services
{
	public interface IApplicationService
	{
		void CloseApplication();

		Task<bool> OpenInNewWindowAsync(string path);
	}
}
