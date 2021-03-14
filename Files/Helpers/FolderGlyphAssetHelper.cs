using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace Files.Helpers
{
    public static class FolderGlyphAssetHelper
    {
        public static async Task<(Uri Path150x150, Uri Path71x71)> GenerateAssetsAsync(string glyph = "")
        {
            var small = await GenerateAssetAsync(250, 250, 56, 0.5f, glyph);
            var medium = await GenerateAssetAsync(300, 300, 42, 0.4f, glyph);
            return (medium, small);
        }

        public static async Task<Uri> GenerateAssetAsync(float widthDpi, float heightDpi, float fontSize, float scale, string glyph = "")
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
            ds.DrawSvg(canvasSvgDocument, new Size(width, height), (width - 256 * scale) / scale / 2, (height - 256 * scale) / scale / 2);
            ds.Transform = Matrix3x2.CreateTranslation(0, 0);

            // skip this step if the glyph is empty
            if(!string.IsNullOrEmpty(glyph))
            {
                // Only use segoe fluent icons if the machine has them installed
                var font = CanvasFontSet.GetSystemFontSet().Fonts.Any(f => f.FamilyNames.Values.Contains("Segoe Fluent Icons")) ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
                ds.DrawText(glyph, width / 2, height * 0.53f, Colors.Black, new Microsoft.Graphics.Canvas.Text.CanvasTextFormat()
                {
                    FontFamily = font,
                    FontSize = fontSize,
                    HorizontalAlignment = Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment.Center,
                    VerticalAlignment = Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment.Center,
                    FontWeight = Windows.UI.Text.FontWeights.Medium,
                });
            }

            ds.Flush();

            var name = $"tile-{(!string.IsNullOrEmpty(glyph) ? (int)glyph[0] : 0)}-{widthDpi}x{heightDpi}.png";
            var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Tiles", CreationCollisionOption.OpenIfExists);
            var saveFile = await saveFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
            using var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite);
            await offscreen.SaveAsync(fileStream, CanvasBitmapFileFormat.Png);
            await fileStream.FlushAsync();
            return new Uri($"ms-appdata:///local/Tiles/{name}");
        }
    }
}
