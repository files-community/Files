using System;

namespace Files.Backend.Models.Coloring
{
    [Serializable]
    public abstract class ColorModel : ICustomFormattable
    {
        public string? FormatInfo { get; set; }
    }
}
