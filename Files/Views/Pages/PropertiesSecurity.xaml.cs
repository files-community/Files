using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            return await Task.FromResult(true);
        }

        public override void Dispose()
        {
        }
    }
}
