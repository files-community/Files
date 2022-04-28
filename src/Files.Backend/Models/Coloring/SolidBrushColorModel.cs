namespace Files.Backend.Models.Coloring
{
    public sealed class SolidBrushColorModel : ColorModel
    {
        public string? ColorCode { get; }

        public bool IsFromResource { get; private set; }

        public SolidBrushColorModel(string colorCode)
        {
            this.ColorCode = colorCode;
        }

        public static SolidBrushColorModel FromResource(string resource)
        {
            var brush = new SolidBrushColorModel(resource)
            {
                IsFromResource = true
            };

            return brush;
        }
    }
}
