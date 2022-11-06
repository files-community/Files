using Files.App.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Helpers
{
	public class ExternalResourcesHelper
	{
		public readonly ObservableCollection<AppTheme> Themes = new()
		{
			new AppTheme
			{
				Name = "Default".GetLocalizedResource(),
			},
		};

		public StorageFolder ThemeFolder { get; set; }
		public StorageFolder ImportedThemesFolder { get; set; }

		public string CurrentThemeResources { get; set; }

		public async Task LoadSelectedTheme()
		{
			string bundledThemesPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Files.App", "Themes");
			ThemeFolder = await StorageFolder.GetFolderFromPathAsync(bundledThemesPath);
			ImportedThemesFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);

			if (App.AppSettings.SelectedTheme.Path is not null)
			{
				await TryLoadThemeAsync(App.AppSettings.SelectedTheme);
			}
		}

		/// <summary>
		/// Overrides xaml resources with custom values
		/// </summary>
		/// <param name="UseCompactSpacing"></param>
		public void OverrideAppResources(bool UseCompactSpacing)
		{
			if (UseCompactSpacing)
			{
				Application.Current.Resources["ListItemHeight"] = 24;
				Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = 20;
			}
			else
			{
				Application.Current.Resources["ListItemHeight"] = 36;
				Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = 32;
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
			foreach (var file in (await folder.GetFilesAsync()).Where(x => string.Equals(x.FileType, ".xaml", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (!Themes.Any(t => t.AbsolutePath == file.Path))
				{
					Themes.Add(new AppTheme()
					{
						Name = file.Name[..^5],
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
				if (xaml is not null)
				{
					App.Current.Resources.MergedDictionaries.Add(xaml);
					if (!Themes.Any(t => t.AbsolutePath == theme.AbsolutePath))
					{
						Themes.Add(theme);
					}
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
			if (theme?.Path is null)
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
			if (OldTheme.Path is not null)
			{
				RemoveTheme(OldTheme);
			}

			if (NewTheme.Path is not null)
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
