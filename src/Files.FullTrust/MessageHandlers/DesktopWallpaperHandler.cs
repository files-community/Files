using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Files.Shared.Extensions;
using Newtonsoft.Json.Linq;
using Vanara.PInvoke;

namespace Files.FullTrust.MessageHandlers
{
    public class DesktopWallpaperHandler : Disposable, IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "WallpaperOperation":
                    SetSlideshow(connection, message);
                    break;
            }
            return Task.CompletedTask;
        }

        private static void SetSlideshow(PipeStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("wallpaperop", ""))
            {
                case "SetSlideshow":
                {
                    JArray jMessage = (JArray)message["filepaths"];
                    var filePaths = jMessage.ToObject<string[]>();

                    if (filePaths == null)
                    {
                        return;
                    }

                    // Create IShellItemArray
                    var idList = filePaths.Select(Shell32.IntILCreateFromPath).ToArray();
                    Shell32.SHCreateShellItemArrayFromIDLists((uint)idList.Length, idList.ToArray(), out var shellItemArray);

                    // Set SlideShow
                    var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
                    wallpaper.SetSlideshow(shellItemArray);

                    // Set wallpaper to fill desktop.
                    wallpaper.SetPosition(Shell32.DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);

                    // TODO: Handle multiple monitors?
                    // var monitors = wallpaper.GetMonitorDevicePathCount();
                    wallpaper.GetMonitorDevicePathAt(0, out var monitorId);
                    // Advance the slideshow to reflect the change.
                    wallpaper.AdvanceSlideshow(monitorId, Shell32.DESKTOP_SLIDESHOW_DIRECTION.DSD_FORWARD);

                    break;
                }
            }
        }
    }
}
