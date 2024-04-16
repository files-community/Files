﻿using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    public class HtmlPreviewViewModel : BasePreviewModel
    {
        private string textValue = string.Empty;
        public string TextValue
        {
            get => textValue;
            private set => SetProperty(ref textValue, value);
        }

        public HtmlPreviewViewModel(ListedItem item) : base(item) {}

        public static bool ContainsExtension(string extension)
            => extension is ".htm" or ".html" or ".svg";

        public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            TextValue = await ReadFileAsTextAsync(Item.ItemFile);
            return new List<FileProperty>();
        }
    }
}