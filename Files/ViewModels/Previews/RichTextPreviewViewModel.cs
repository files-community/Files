using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Files.ViewModels.Previews
{
    public class RichTextPreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>() {
            ".rtf"
        };

        public IRandomAccessStream Stream { get; set; }

        public RichTextPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public async override void LoadPreviewAndDetails()
        {
            try
            {
                Stream = await ItemFile.OpenReadAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            LoadSystemFileProperties();
            RaiseLoadedEvent();
        }
    }
}
