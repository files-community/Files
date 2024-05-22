// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Globalization;
using Windows.Globalization;

namespace Files.App.Helpers
{
    /// <summary>
    /// A helper class for managing application languages.
    /// </summary>
    public static class AppLanguageHelper
    {
		/// <summary>
		/// A constant string representing the default language ID.
		/// It is initialized as an empty string.
		/// </summary>
		private static readonly string _defaultId = string.Empty;

        /// <summary>
        /// A collection of available languages.
        /// </summary>
        public static ObservableCollection<AppLanguageItem> Languages { get; }

        /// <summary>
        /// The index of the currently selected language.
        /// </summary>
        public static int SelectedIndex { get; private set; }

        /// <summary>
        /// The ID of the currently selected language.
        /// </summary>
        public static string SelectedLanguage => Languages[SelectedIndex].Id;

        /// <summary>
        /// Initializes the AppLanguageHelper class.
        /// </summary>
        static AppLanguageHelper()
        {
            if (Languages is not null)
                return;

            // Populate the Languages collection with available languages
            var appLanguages = ApplicationLanguages.ManifestLanguages
               .Append(string.Empty) // Add default language id
               .Select(language => new AppLanguageItem(language))
               .OrderBy(language => language.Id is not "") // Default language on top
               .ThenBy(language => language.Name)
               .ToList();

			// Get the current primary language override.
			var current = new AppLanguageItem(ApplicationLanguages.PrimaryLanguageOverride);

            // Find the index of the saved language
            var index = appLanguages.IndexOf(appLanguages.FirstOrDefault(dl => dl.Name == current.Name)?? appLanguages.First());
            SelectedIndex = index;

            // Set the OS default language as the first item in the Languages collection
            var osLanguage = new AppLanguageItem(CultureInfo.InstalledUICulture.Name, osDefault: true);
            if (appLanguages.Select(lang => lang.Name.Contains(osLanguage.Name)).Any())
                appLanguages[0] = osLanguage;
            else
                appLanguages[0] = new("en-US", osDefault: true);

            // Initialize the Languages collection
            Languages = new ObservableCollection<AppLanguageItem>(appLanguages);
        }

        /// <summary>
        /// Attempts to change the selected language by index.
        /// </summary>
        /// <param name="index">The index of the new language.</param>
        /// <returns>True if the language was successfully changed; otherwise, false.</returns>
        public static bool TryChangeIndex(int index)
        {
            if (index == SelectedIndex)
                return false;

            // Update the primary language override
            ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultId : Languages[index].Id;

            SelectedIndex = index;
            return true;
        }

        /// <summary>
        /// Attempts to change the selected language by ID.
        /// </summary>
        /// <param name="id">The ID of the new language.</param>
        /// <returns>True if the language was successfully changed; otherwise, false.</returns>
        public static bool TryChangeId(string id)
        {
            var lang = new AppLanguageItem(id);
            var index = Languages
				.Skip(1) // Skip first (default) language
				.ToList()
				.IndexOf(Languages.FirstOrDefault(dl => dl.Name == lang.Name) ?? Languages.First());

			// Adjusts the index to avoid zero (default language), increments if zero
			index = index == 0 ? index : index + 1;

            if (index == SelectedIndex)
                return false;

            // Update the primary language override
            ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultId : id;

            SelectedIndex = index;
            return true;
        }
    }

    /// <summary>
    /// Represents a language in the application.
    /// </summary>
    public sealed class AppLanguageItem
    {
        /// <summary>
        /// The ID of the language.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the language.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the AppLanguageItem class.
        /// </summary>
        /// <param name="langId">The ID of the language.</param>
        /// <param name="osDefault">Indicates whether the language is the OS default.</param>
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