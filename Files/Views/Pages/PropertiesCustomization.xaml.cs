using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PropertiesCustomization : PropertiesTab
    {
        public PropertiesCustomization()
        {
            this.InitializeComponent();
        }

        private async void CustomIconsSelectorFrame_Loaded(object sender, RoutedEventArgs e)
        {
            string initialPath = @"C:\Windows\System32\SHELL32.dll";

            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "GetFolderIconsFromDLL" },
                    { "iconFile", initialPath }
                });
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("IconInfos"))
                {
                    var icons = JsonConvert.DeserializeObject<IList<IconFileInfo>>(response["IconInfos"] as string);
                    (sender as Frame).Navigate(typeof(CustomFolderIcons), new IconSelectorInfo {
                        Icons = icons,
                        InitialPath = initialPath,
                        SelectedDirectory = (BaseProperties as FolderProperties)?.Item.ItemPath
                    },
                    new SuppressNavigationTransitionInfo());
                }
            }
        }

        public override async Task<bool> SaveChangesAsync(ListedItem item)
        {
            return await Task.FromResult(true);
        }

        public override void Dispose()
        {
        }

        public class IconSelectorInfo
        {
            public string SelectedDirectory;
            public IList<IconFileInfo> Icons;
            public string InitialPath;
        }
    }
}
