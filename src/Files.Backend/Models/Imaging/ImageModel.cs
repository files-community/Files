#nullable enable

namespace Files.Backend.Models.Imaging
{
    public abstract class ImageModel : ICustomFormattable
    {
        public string? FormatInfo { get; set; }
    }
}
