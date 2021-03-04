using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Files.Helpers
{
    static class ExternalResourcesHelper
    {
        public static async Task LoadThemeFromAppData()
        {
            try
            {
                var themefolder = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("themes")) as StorageFolder;
                themefolder ??= await ApplicationData.Current.LocalFolder.CreateFolderAsync("themes");
                var file = (await themefolder.GetFilesAsync()).Where(x => x.FileType == ".xaml").FirstOrDefault();
                var text = await FileIO.ReadTextAsync(file);
                var dict = XamlReader.Load(text) as ResourceDictionary;

                App.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception)
            {
            }

        }
    }
}
