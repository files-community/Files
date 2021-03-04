using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Files.Helpers
{
    public class ExternalResourcesHelper
    {
        public List<AppTheme> Themes { get; set; } = new List<AppTheme>();
        public async Task LoadThemesAsync()
        {
            try
            {
                var themefolder = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("themes")) as StorageFolder;
                themefolder ??= await ApplicationData.Current.LocalFolder.CreateFolderAsync("themes");
                foreach (var file in (await themefolder.GetFilesAsync()).Where(x => x.FileType == ".xaml"))
                {
                    var text = await FileIO.ReadTextAsync(file);
                    var theme = new AppTheme { ResourceDictionary = XamlReader.Load(text) as ResourceDictionary, Name = file.Name.Replace(".xaml", "") };
                    Themes.Add(theme);
                    App.Current.Resources.MergedDictionaries.Add(theme.ResourceDictionary);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error loading themes");
            }
        }

        public struct AppTheme
        {
            public ResourceDictionary ResourceDictionary { get; set; }
            public string Name { get; set; }
        }
    }
}
