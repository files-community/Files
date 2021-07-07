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
        public List<AppSkin> Skins = new List<AppSkin>()
        {
            new AppSkin
            {
                Name = "DefaultSkin".GetLocalized(),
            },
        };

        public StorageFolder SkinFolder { get; set; }
        public StorageFolder OptionalPackageSkinFolder { get; set; }

        public string CurrentSkinResources { get; set; }

        public async Task LoadSelectedSkin()
        {
            if (App.OptionalPackageManager.TryGetOptionalPackage(Constants.OptionalPackages.SkinsOptionalPackagesName, out var package))
            {
                Debug.WriteLine(package.InstalledLocation.Path);
                OptionalPackageSkinFolder = package.InstalledLocation;
            }

            try
            {
                // ToDo this is for backwards compatability, remove after a couple updates
                var themeFolder = await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\\Themes");
                await themeFolder.RenameAsync("Skins", NameCollisionOption.FailIfExists);
            }
            catch (Exception)
            {
            }

            SkinFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Skins", CreationCollisionOption.OpenIfExists);

            // This is used to migrate to the new skin setting
            // It can be removed in a future update
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("PathToSkinFile", out var path))
            {
                var pathStr = path as string;
                App.AppSettings.SelectedSkin = new AppSkin()
                {
                    Name = pathStr.Replace(".xaml", ""),
                    Path = pathStr,
                };
                ApplicationData.Current.LocalSettings.Values.Remove("PathToSkinFile");
            }

            if (App.AppSettings.SelectedSkin.Path != null)
            {
                await TryLoadSkinAsync(App.AppSettings.SelectedSkin);
            }

            LoadOtherSkinsAsync();
        }

        private async void LoadOtherSkinsAsync()
        {
            try
            {
                await AddSkinsAsync(SkinFolder);

                if (OptionalPackageSkinFolder != null)
                {
                    await AddSkinsAsync(OptionalPackageSkinFolder, true);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error loading skins");
            }
        }

        private async Task AddSkinsAsync(StorageFolder folder, bool isOptionalPackage = false)
        {
            foreach (var file in (await folder.GetFilesAsync()).Where(x => x.FileType == ".xaml"))
            {
                Skins.Add(new AppSkin()
                {
                    Name = file.Name.Replace(".xaml", ""),
                    Path = file.Name,
                    IsFromOptionalPackage = isOptionalPackage
                });
            }
        }

        public async Task<bool> TryLoadSkinAsync(AppSkin skin)
        {
            try
            {
                StorageFile file;
                if (skin.IsFromOptionalPackage)
                {
                    if (OptionalPackageSkinFolder != null)
                    {
                        file = await OptionalPackageSkinFolder.GetFileAsync(skin.Path);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    file = await SkinFolder.GetFileAsync(skin.Path);
                }
                CurrentSkinResources = await FileIO.ReadTextAsync(file);
                var xaml = XamlReader.Load(CurrentSkinResources) as ResourceDictionary;
                xaml.Add("CustomSkinID", skin.Key);
                App.Current.Resources.MergedDictionaries.Add(xaml);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async void UpdateSkin(AppSkin OldSkin, AppSkin NewSkin)
        {
            if (OldSkin.Path != null)
            {
                RemoveSkin(OldSkin);
            }

            if (NewSkin.Path != null)
            {
                await TryLoadSkinAsync(NewSkin);
            }
        }

        public bool RemoveSkin(AppSkin skin)
        {
            try
            {
                App.Current.Resources.MergedDictionaries.Remove(App.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.TryGetValue("CustomSkinID", out var key) && (key as string) == skin.Key));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public struct AppSkin
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsFromOptionalPackage { get; set; }
        public string Key => $"{Name}-{IsFromOptionalPackage}";
    }
}