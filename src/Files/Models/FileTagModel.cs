using System;
using Files.Backend.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;

namespace Files.Models
{
    internal class FileTagModel : IFileTag
    {
        public string TagName { get; set; }
        public string Uid { get; set; }
        public string ColorString { get; set; }

        private SolidColorBrush color;

        [JsonIgnore]
        public SolidColorBrush Color => color ??= new SolidColorBrush(ColorString.ToColor());

        public FileTagModel(string tagName, string colorString)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = Guid.NewGuid().ToString();
        }

        [JsonConstructor]
        public FileTagModel(string tagName, string colorString, string uid)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = uid;
        }
    }
}
