using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Files.ViewModels.Previews
{
    public class BasicPreviewViewModel : BasePreviewModel
    {
        public BasicPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var icon = await Item.ItemFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 300, ThumbnailOptions.UseCurrentScale);
            if(icon != null) 
            { 
                await Item.FileImage.SetSourceAsync(icon);
            }
            return new List<FileProperty>();
        }
    }
}
