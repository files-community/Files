using Files.Backend.Services;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Files.ServicesImplementation
{
    internal sealed class ApplicationService : IApplicationService
    {
        public void CloseApplication()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> OpenInNewWindowAsync(string path)
        {
            var folderUri = new Uri($"files-uwp:?folder={Uri.EscapeDataString(path)}");
            return await Launcher.LaunchUriAsync(folderUri);
        }
    }
}
