﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        /// Gets a unique tile-id to be used from a folder path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetTileID(string path)
        {
            // Remove symbols because windows doesn't like them in the ID, and will blow up
            var str = $"folder-{new string(path.Where(c => char.IsLetterOrDigit(c)).ToArray())}";
            if (str.Length > 64)
            {
                // if the id string is too long, Windows will throw an error, so remove every other character
                str = new string(str.Where((x, i) => i % 2 == 0).ToArray());
            }
            return str;
        }

        public async Task<bool> TryPinFolderAsync(string path, string name)
        {
            var result = false;
            try
            {
                Uri Path150x150 = new Uri("ms-appx:///Assets/tile-0-300x300.png");
                Uri Path71x71 = new Uri("ms-appx:///Assets/tile-0-250x250.png");

                SecondaryTile tile = new SecondaryTile(
                    GetTileID(path),
                    name,
                    path,
                    Path150x150,
                    TileSize.Square150x150);

                tile.VisualElements.Square71x71Logo = Path71x71;
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