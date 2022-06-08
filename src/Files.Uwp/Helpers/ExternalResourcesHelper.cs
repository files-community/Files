using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Files.Uwp.Helpers
{
    public class ExternalResourcesHelper
    {
        public List<AppTheme> Themes = new List<AppTheme>()
        {
            new AppTheme
            {
                Name = "Default".GetLocalized(),
            },
        };

        public StorageFolder ThemeFolder { get; set; }
        public StorageFolder ImportedThemesFolder { get; set; }

        public string CurrentThemeResources { get; set; }

        public async Task LoadSelectedTheme()
        {
            StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            ThemeFolder = await appInstalledFolder.GetFolderAsync("Themes");
            ImportedThemesFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);

            if (App.AppSettings.SelectedTheme.Path != null)
            {
                await TryLoadThemeAsync(App.AppSettings.SelectedTheme);
            }
        }

        public async Task LoadOtherThemesAsync()
        {
            try
            {
                await AddThemesAsync(ThemeFolder);
                await AddThemesAsync(ImportedThemesFolder);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error loading themes");
            }
        }

        private async Task AddThemesAsync(StorageFolder folder)
        {
            foreach (var file in (await folder.GetFilesAsync()).Where(x => x.FileType == ".xaml"))
            {
                if(!Themes.Exists(t => t.AbsolutePath == file.Path))
                {
                    Themes.Add(new AppTheme()
                    {
                        Name = file.Name.Replace(".xaml", "", StringComparison.Ordinal),
                        Path = file.Name,
                        AbsolutePath = file.Path,
                    });
                }
            }
        }

        public async Task<bool> TryLoadThemeAsync(AppTheme theme)
        {
            try
            {
                var xaml = await TryLoadResourceDictionary(theme);
                if (xaml != null)
                {
                    App.Current.Resources.MergedDictionaries.Add(xaml);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, $"Error loading theme: {theme?.Path}");
                return false;
            }
        }

        public async Task<ResourceDictionary> TryLoadResourceDictionary(AppTheme theme)
        {
            StorageFile file;
            if (theme?.Path == null)
            {
                return null;
            }

            if (theme.AbsolutePath.Contains(ImportedThemesFolder.Path))
            {
                file = await ImportedThemesFolder.GetFileAsync(theme.Path);
                theme.IsImportedTheme = true;
            }
            else
            {
                file = await ThemeFolder.GetFileAsync(theme.Path);
                theme.IsImportedTheme = false;
            }

            var code = await FileIO.ReadTextAsync(file);
            var xaml = XamlReader.Load(code) as ResourceDictionary;
            xaml.Add("CustomThemeID", theme.Key);
            return xaml;
        }

        public async Task UpdateTheme(AppTheme OldTheme, AppTheme NewTheme)
        {
            if (OldTheme.Path != null)
            {
                RemoveTheme(OldTheme);
            }

            if (NewTheme.Path != null)
            {
                await TryLoadThemeAsync(NewTheme);
            }
        }

        public bool RemoveTheme(AppTheme theme)
        {
            try
            {
                App.Current.Resources.MergedDictionaries.Remove(App.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.TryGetValue("CustomThemeID", out var key) && (key as string) == theme.Key));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class AppTheme
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string AbsolutePath { get; set; }
        public string Key => $"{Name}";
        public bool IsImportedTheme { get; set; }
    }
}