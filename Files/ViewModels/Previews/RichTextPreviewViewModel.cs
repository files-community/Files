using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Files.ViewModels.Previews
{
    public class RichTextPreviewViewModel : BasePreviewModel
    {
        public RichTextPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            ".rtf"
        };

        public IRandomAccessStream Stream { get; set; }

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            Stream = await Item.ItemFile.OpenReadAsync();
            return new List<FileProperty>();
        }
    }
}