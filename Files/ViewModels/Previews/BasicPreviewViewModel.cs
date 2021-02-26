using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage.FileProperties;
using Files.Common;

namespace Files.ViewModels.Previews
{
    public class BasicPreviewViewModel : BasePreviewModel
    {
        public BasicPreviewViewModel(ListedItem item) : base(item)
        {
        }
    }
}
