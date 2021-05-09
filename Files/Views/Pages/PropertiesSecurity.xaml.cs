using Files.Filesystem;
using Files.Filesystem.Security;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public PropertiesSecurity()
        {
            this.InitializeComponent();
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            return false;
        }
        public override void Dispose()
        {
        }
    }
}