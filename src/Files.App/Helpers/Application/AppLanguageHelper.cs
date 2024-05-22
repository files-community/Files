using System.Globalization;
using Windows.Globalization;

namespace Files.App.Helpers
{
	public static class AppLanguageHelper
	{
		private static readonly string _defaultId = string.Empty;

		private static readonly ObservableCollection<AppLanguageItem> _appLanguages;
		public static ObservableCollection<AppLanguageItem> AppLanguages => _appLanguages;

		private static int _selectedLanguageIndex;
		public static int SelectedLanguageIndex => _selectedLanguageIndex;

		public static string SelectedLanguage => AppLanguages[SelectedLanguageIndex].Id;

		static AppLanguageHelper()
		{
			if (_appLanguages is not null)
				return;

			var appLanguages = ApplicationLanguages.ManifestLanguages
			.Append(string.Empty) // Add default language id
			.Select(language => new AppLanguageItem(language))
			.OrderBy(language => language.Id is not "") // Default language on top
			.ThenBy(language => language.Name)
			.ToList();

			var save = new AppLanguageItem(ApplicationLanguages.PrimaryLanguageOverride);

			var index = appLanguages.IndexOf(appLanguages.FirstOrDefault(dl => dl.Name == save.Name) ?? appLanguages.First());
			_selectedLanguageIndex = index;

			var osLanguage = new AppLanguageItem(CultureInfo.InstalledUICulture.Name, osDefault: true);
			if (appLanguages.Select(lang => lang.Name.Contains(osLanguage.Name)).Any())
				appLanguages[0] = osLanguage;
			else
				appLanguages[0] = new("en-US", osDefault: true);

			_appLanguages = new ObservableCollection<AppLanguageItem>(appLanguages);
		}

		public static bool TryChangeIndex(int index)
		{
			if (index == _selectedLanguageIndex)
				return false;

			ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultId : _appLanguages[index].Id;

			_selectedLanguageIndex = index;
			return true;
		}

		public static bool TryChangeId(string id)
		{
			var lang = new AppLanguageItem(id);
			var index = _appLanguages.Skip(1).ToList().IndexOf(_appLanguages.FirstOrDefault(dl => dl.Name == lang.Name) ?? _appLanguages.First());
			index = index == 0 ? index : index + 1;

			if (index == _selectedLanguageIndex)
				return false;

			ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultId : id;

			_selectedLanguageIndex = index;
			return true;
		}
	}

	public sealed class AppLanguageItem
    {
        public string Id { get; set; }

		public string Name { get; set; }

        public AppLanguageItem(string langId, bool osDefault = false)
		{
			if (osDefault || string.IsNullOrEmpty(langId))
			{
				Id = new CultureInfo(langId).Name;
				Name = "SettingsPreferencesSystemDefaultLanguageOption".GetLocalizedResource();
			}
			else
			{
				var culture = new CultureInfo(langId);
				Id = culture.Name;
				Name = culture.NativeName;
			}
		}

		public override string ToString() => Name;
	}
}
