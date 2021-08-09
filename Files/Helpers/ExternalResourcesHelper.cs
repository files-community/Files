using Microsoft.Toolkit.Uwp;
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
        public List<AppTheme> Themes = new List<AppTheme>()
        {
            new AppTheme
            {
                Name = "DefaultTheme".GetLocalized(),
            },
        };

        public StorageFolder ThemeFolder { get; set; }
        public StorageFolder OptionalPackageThemesFolder { get; set; }

        public string CurrentThemeResources { get; set; }

        public async Task LoadSelectedTheme()
        {
            if (App.OptionalPackageManager.TryGetOptionalPackage(Constants.OptionalPackages.ThemesOptionalPackagesName, out var package))
            {
                Debug.WriteLine(package.InstalledLocation.Path);
                OptionalPackageThemesFolder = package.InstalledLocation;
            }

            ThemeFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);

            // This is used to migrate to the new theme setting
            // It can be removed in a future update
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("PathToThemeFile", out var path))
            {
                var pathStr = path as string;
                App.AppSettings.SelectedTheme = new AppTheme()
                {
                    Name = pathStr.Replace(".xaml", ""),
                    Path = pathStr,
                };
                ApplicationData.Current.LocalSettings.Values.Remove("PathToThemeFile");
            }

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

                if (OptionalPackageThemesFolder != null)
                {
                    await AddThemesAsync(OptionalPackageThemesFolder, true);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error loading themes");
            }
        }

        private async Task AddThemesAsync(StorageFolder folder, bool isOptionalPackage = false)
        {
            foreach (var file in (await folder.GetFilesAsync()).Where(x => x.FileType == ".xaml"))
            {
                Themes.Add(new AppTheme()
                {
                    Name = file.Name.Replace(".xaml", ""),
                    Path = file.Name,
                    AbsolutePath = file.Path,
                    IsFromOptionalPackage = isOptionalPackage
                });
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
            if (theme.IsFromOptionalPackage)
            {
                if (OptionalPackageThemesFolder != null)
                {
                    file = await OptionalPackageThemesFolder.GetFileAsync(theme.Path);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                file = await ThemeFolder.GetFileAsync(theme.Path);
            }
            var code = await FileIO.ReadTextAsync(file);
            var xaml = XamlReader.Load(code) as ResourceDictionary;
            xaml.Add("CustomThemeID", theme.Key);
            return xaml;
        }

        public async void UpdateTheme(AppTheme OldTheme, AppTheme NewTheme)
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
        public bool IsFromOptionalPackage { get; set; }
        public string Key => $"{Name}-{IsFromOptionalPackage}";
    }
}