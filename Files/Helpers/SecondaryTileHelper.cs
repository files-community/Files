using Files.Filesystem;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
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
            var file = await GenerateAssetAsync(glyph, GetTileID(name));

            SecondaryTile tile = new SecondaryTile(
                GetTileID(path),
                name,
                path,
                new Uri("ms-appx:///Assets/Tiles/Files Icon.png"),
                TileSize.Default);

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveImage()
                                {
                                    Source = $"{file.Path}",
                                }
                            }
                        }
                    }
                }
            };


            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            var result = await tile.RequestCreateAsync();

            // Generate the tile notification content and update the tile
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(new TileNotification(content.GetXml()));
            return result;
        }

        public async Task<StorageFile> GenerateAssetAsync(string glyph, string id)
        {
            var image = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Tiles/TileBaseLogo.png"));
            
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget offscreen = new CanvasRenderTarget(device, 259, 229, 32);
            using CanvasDrawingSession ds = offscreen.CreateDrawingSession();
            ds.Clear(Colors.Transparent);
            var thing = await CanvasBitmap.LoadAsync(device, await image.OpenAsync(FileAccessMode.Read));
            ds.DrawImage(thing);
            ds.DrawText(glyph, 60, 60, Colors.Black, new Microsoft.Graphics.Canvas.Text.CanvasTextFormat()
            {
                FontFamily = "Segoe Fluent Icons",
                FontSize = 90
            });

            ds.Flush();
            var saveFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync($"{id}.png", CreationCollisionOption.ReplaceExisting);
            await offscreen.SaveAsync(await saveFile.OpenAsync(FileAccessMode.ReadWrite), CanvasBitmapFileFormat.Png);
            return saveFile;
        }
    }
}
