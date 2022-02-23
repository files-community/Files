using Files.Backend.Models.Coloring;
using Newtonsoft.Json;
using System;

#nullable enable

namespace Files.Backend.ViewModels.FileTags
{
    [Serializable]
    public sealed class FileTagViewModel
    {
        public string TagName { get; set; }

        public string Uid { get; set; }

        public string ColorString { get; set; }

        [JsonIgnore]
        private ColorModel? _ColorModel;

        [JsonIgnore]
        public ColorModel ColorModel => _ColorModel ??= new SolidBrushColorModel(ColorString);

        public FileTagViewModel(string tagName, string colorString)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = Guid.NewGuid().ToString();
        }

        [JsonConstructor]
        public FileTagViewModel(string tagName, string colorString, string uid)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = uid;
        }
    }
}
