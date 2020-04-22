using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.IO;
using Files.Filesystem;
using Newtonsoft.Json;
using Files.DataModels;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using System.Linq;

namespace Files.SettingsPages
{
    public sealed partial class Experimental : Page
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public Experimental()
        {
            this.InitializeComponent();
        }
    }
}