#nullable enable

namespace Files.Backend.Models.Imaging
{
    public sealed class ReadyBitmapImageModel : BitmapImageModel
    {
        private readonly object _image;

        public ReadyBitmapImageModel(object image)
        {
            this._image = image;
        }

        public override object GetImage()
        {
            return _image;
        }
    }
}
