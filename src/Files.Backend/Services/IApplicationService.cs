using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IApplicationService
    {
        void CloseApplication();

        Task<bool> OpenInNewWindowAsync(string path);
    }
}
