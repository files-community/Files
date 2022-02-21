using System;
using Newtonsoft.Json;

namespace Files.Models
{
    [Serializable]
    public sealed class FileTagModel
    {
        public string TagName { get; set; }

        public string Uid { get; set; }

        public string Color { get; set; }

        public FileTagModel(string tagName, string color)
        {
            TagName = tagName;
            Color = color;
            Uid = Guid.NewGuid().ToString();
        }

        [JsonConstructor]
        private FileTagModel(string tagName, string color, string uid)
        {
            TagName = tagName;
            Color = color;
            Uid = uid;
        }
    }
}
