using System.Collections.Generic;

namespace Files.Backend.Models.Imaging
{
    public abstract class BitmapImageModel : ImageModel
    {
        public override IReadOnlyCollection<string>? Formats { get; }

        public BitmapImageModel()
        {
            Formats = new List<string>()
            {
                Constants.KnownImageFormats.BITMAP_IMAGE_FORMAT
            };
        }

        public abstract object GetImage();
    }
}
