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
                Name = "DefaultScheme".GetLocalized(),
            },
        };

        public StorageFolder ThemeFolder { get; set; }
        public StorageFolder OptionalPackageThemeFolder { get; set; }

        public string CurrentThemeResources { get; set; }

        public async Task LoadSelectedTheme()
        {
            if (App.OptionalPackageManager.TryGetOptionalPackage(Constants.OptionalPackages.ThemesOptionalPackagesName, out var package))
            {
                Debug.WriteLine(package.InstalledLocation.Path);
                OptionalPackageThemeFolder = package.InstalledLocation;
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

            LoadOtherThemesAsync();
        }

        private async void LoadOtherThemesAsync()
        {
            try
            {
                await AddThemesAsync(ThemeFolder);

                if (OptionalPackageThemeFolder != null)
                {
                    await AddThemesAsync(OptionalPackageThemeFolder, true);
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
                    IsFromOptionalPackage = isOptionalPackage
                });
            }
        }

        public async Task<bool> TryLoadThemeAsync(AppTheme theme)
        {
            try
            {
                StorageFile file;
                if (theme.IsFromOptionalPackage)
                {
                    if (OptionalPackageThemeFolder != null)
                    {
                        file = await OptionalPackageThemeFolder.GetFileAsync(theme.Path);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    file = await ThemeFolder.GetFileAsync(theme.Path);
                }
                CurrentThemeResources = await FileIO.ReadTextAsync(file);
                var xaml = XamlReader.Load(CurrentThemeResources) as ResourceDictionary;
                xaml.Add("CustomThemeID", theme.Key);
                App.Current.Resources.MergedDictionaries.Add(xaml);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async void UpdateTheme(AppTheme OldTheme, AppTheme NewTheme)
        {
            if (OldTheme.Path != null)
            {
                RemoveTheme(OldTheme);
            }

            if(NewTheme.Path != null)
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

    public struct AppTheme
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsFromOptionalPackage { get; set; }
        public string Key => $"{Name}-{IsFromOptionalPackage}";
    }
}
