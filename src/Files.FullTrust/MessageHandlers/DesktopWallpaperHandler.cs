using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Files.Shared.Extensions;
using Vanara.PInvoke;

namespace Files.FullTrust.MessageHandlers
{
    public class DesktopWallpaperHandler : Disposable, IMessageHandler
    {
        private static readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public void Initialize(PipeStream connection)
        {
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            switch (arguments)
            {
                case "WallpaperOperation":
                    SetSlideshow(connection, message);
                    break;
            }
            return Task.CompletedTask;
        }

        private static void SetSlideshow(PipeStream connection, Dictionary<string, JsonElement> message)
        {
            switch (message.Get("wallpaperop", defaultJson).GetString())
            {
                case "SetSlideshow":
                {
                    JsonArray jMessage = JsonArray.Create(message["filepaths"]);
                    var filePaths = jMessage.Select(x => x.ToString()).ToArray();

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
