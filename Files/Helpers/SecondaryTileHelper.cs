using Files.Filesystem;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
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
            // Remove symbols because windows doesn't like them in the ID, and will blow up
            return $"folder-{new string(path.Where(c => char.IsLetterOrDigit(c)).ToArray())}";
        }

        public async Task<bool> TryPinFolderAsync(string path, string name)
        {
            var result = false;
            try
            {
                var glyph = GlyphHelper.GetItemIcon(path);
                // ignore the default
                if (glyph == "\uE8B7")
                {
                    glyph = "";
                }

                (Uri Path150x150, Uri Path71x71) logos = await FolderGlyphAssetHelper.GenerateAssetsAsync(glyph);

                SecondaryTile tile = new SecondaryTile(
                    GetTileID(path),
                    name,
                    path,
                    logos.Path150x150,
                    TileSize.Square150x150);

                tile.VisualElements.Square71x71Logo = logos.Path71x71;
                tile.VisualElements.ShowNameOnSquare150x150Logo = true;
                result = await tile.RequestCreateAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(GetTileID(path));
                Debug.WriteLine(e.ToString());
            }

            return result;
        }

        public async Task<bool> UnpinFromStartAsync(string path)
        {
            return await StartScreenManager.GetDefault().TryRemoveSecondaryTileAsync(GetTileID(path));
        }
    }
}
