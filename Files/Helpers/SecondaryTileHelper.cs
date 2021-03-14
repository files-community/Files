using Files.Filesystem;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
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
            return $"folder-{path.Replace("\\", "").Replace(":", "")}";
        }

        public async Task<bool> PinFolderAsync(string path, string name, string glyph)
        {

            (Uri Path150x150, Uri Path71x71) logos = await GenerateAssetsAsync(glyph);

            SecondaryTile tile = new SecondaryTile(
                GetTileID(path),
                name,
                path,
                logos.Path150x150,
                TileSize.Square150x150);

            tile.VisualElements.Square71x71Logo = logos.Path71x71;
            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            var result = await tile.RequestCreateAsync();

            return result;
        }

        public async Task<(Uri Path150x150, Uri Path71x71)> GenerateAssetsAsync(string glyph)
        {
            var small = await GenerateAssetAsync(glyph, 250, 250, 56, 0.5f);
            var medium = await GenerateAssetAsync(glyph, 300, 300, 42, 0.4f);
            return (medium, small);
        }

        public async Task<Uri> GenerateAssetAsync(string glyph, float widthDpi, float heightDpi, float fontSize, float scale)
        {
            var image = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FolderIcon2Large.svg"));
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget offscreen = new CanvasRenderTarget(device, widthDpi, heightDpi, 64);
            var width = offscreen.SizeInPixels.Width;
            var height = offscreen.SizeInPixels.Height;
            using CanvasDrawingSession ds = offscreen.CreateDrawingSession();
            ds.Clear(Colors.Transparent);
            ds.Units = CanvasUnits.Pixels;
            ds.Antialiasing = CanvasAntialiasing.Antialiased;
            ds.TextAntialiasing = Microsoft.Graphics.Canvas.Text.CanvasTextAntialiasing.ClearType;
            var canvasSvgDocument = await CanvasSvgDocument.LoadAsync(device, await image.OpenReadAsync());
            ds.Transform *= Matrix3x2.CreateScale(scale, scale);
            ds.DrawSvg(canvasSvgDocument, new Size(width, height), (width-256*scale)/scale/2, (height-256*scale)/scale/2);
            ds.Transform = Matrix3x2.CreateTranslation(0, 0);
            ds.DrawText(glyph, width/2, height*0.53f, Colors.Black, new Microsoft.Graphics.Canvas.Text.CanvasTextFormat()
            {
                FontFamily = "Segoe Fluent Icons",
                FontSize = fontSize,
                HorizontalAlignment = Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment.Center,
                VerticalAlignment = Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment.Center,
                FontWeight = Windows.UI.Text.FontWeights.Medium,
            });
            ds.Flush();

            var name = $"tile-{(int)glyph[0]}-{widthDpi}x{heightDpi}.png";

            var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Tiles", CreationCollisionOption.OpenIfExists);
            var saveFile = await saveFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            await offscreen.SaveAsync(await saveFile.OpenAsync(FileAccessMode.ReadWrite), CanvasBitmapFileFormat.Png);
            return new Uri($"ms-appdata:///local/Tiles/{name}");
        }
    }
}
