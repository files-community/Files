using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Files.Helpers
{
    public class ExternalResourcesHelper
    {
        public List<string> Themes = new List<string>()
        {
            "DefaultScheme".GetLocalized()
        };

        private StorageFolder ThemeFolder { get; set; }

        public string CurrentThemeResources { get; set; }

        public async Task LoadSelectedTheme()
        {
            ThemeFolder = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("Themes")) as StorageFolder;
            ThemeFolder ??= await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes");

            if (App.AppSettings.PathToThemeFile != "DefaultScheme".GetLocalized())
            {
                await TryLoadThemeAsync(App.AppSettings.PathToThemeFile);
            }

            LoadOtherThemesAsync();
        }

        private async void LoadOtherThemesAsync()
        {
            try
            {
                foreach (var file in (await ThemeFolder.GetFilesAsync()).Where(x => x.FileType == ".xaml"))
                {
                    Themes.Add(file.Name);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error loading themes");
            }
        }

        public async Task<bool> TryLoadThemeAsync(string name)
        {
            try
            {
                var file = await ThemeFolder.GetFileAsync(name);
                CurrentThemeResources = await FileIO.ReadTextAsync(file);
                var xaml = XamlReader.Load(CurrentThemeResources) as ResourceDictionary;
                App.Current.Resources.MergedDictionaries.Add(xaml);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public struct AppTheme
        {
            public ResourceDictionary ResourceDictionary { get; set; }
            public string Name { get; set; }
        }
    }
}