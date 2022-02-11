using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Backend.Helpers
{
    public static class NavigationHelpers
    {
        public static async Task<bool> OpenPathInNewWindowAsync(string path)
        {
            var folderUri = new Uri($"files-uwp:?folder={Uri.EscapeDataString(path)}");
            return await Launcher.LaunchUriAsync(folderUri);
        }
    }
}
