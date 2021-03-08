using Files.Filesystem;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using TileSize = Windows.UI.StartScreen.TileSize;

namespace Files.Helpers
{
    public class SecondaryTileHelper
    {
        public bool CheckFolderPinned(string path)
        {
            return SecondaryTile.Exists(GetTileID(path));
        }

        /// <summary>
        /// Gets a tile-id to be used from a folder path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetTileID(string path)
        {
            return $"folder-{path.Replace("\\", "").Replace(":", "")}";
        }

        public async Task<bool> PinFolderAsync(string path, string name, string glyph)
        {
            var logoPath = new Uri($"ms-appx:///Assets/Tiles/tile-{(int)glyph[0]}.png");
            try
            {
                var logo = await StorageFile.GetFileFromApplicationUriAsync(logoPath);
            } catch
            {
                // Specified icon file does not exist, use default
                logoPath = new Uri($"ms-appx:///Assets/Tiles/tile-0.png");
            }

            SecondaryTile tile = new SecondaryTile(
                GetTileID(path),
                name,
                path,
                logoPath,
                TileSize.Square150x150);

            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            var result = await tile.RequestCreateAsync();

            return result;
        }
    }
}
