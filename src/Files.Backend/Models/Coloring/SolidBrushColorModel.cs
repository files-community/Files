#nullable enable

namespace Files.Backend.Models.Coloring
{
    public sealed class SolidBrushColorModel : ColorModel
    {
        public string? ColorHex { get; }

        public SolidBrushColorModel(string colorHex)
        {
            this.ColorHex = colorHex;
        }
    }
}
