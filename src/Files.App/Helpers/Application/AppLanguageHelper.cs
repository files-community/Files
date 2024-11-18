// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Globalization;
using System.Text.RegularExpressions;
using Windows.Globalization;
using WinRT.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Helpers
{
	// TODO: Replaced by RealTime Resources in the future
	/// <summary>
	/// Provides static helper to manage supported languages in the application.
	/// </summary>
	public static class AppLanguageHelper
	{
		/// <summary>
		/// A constant string representing the default language code.
		/// It is initialized as an empty string.
		/// </summary>
		private static readonly string _defaultCode = string.Empty;

		/// <summary>
		/// A collection of available languages.
		/// </summary>
		public static ObservableCollection<AppLanguageItem> SupportedLanguages { get; }

		/// <summary>
		/// Gets the preferred language.
		/// </summary>
		public static AppLanguageItem PreferredLanguage { get; private set; }

		public static CultureInfo PreferredCulture => new(PreferredLanguage.Code);

		/// <summary>
		/// Initializes the <see cref="AppLanguageHelper"/> class.
		/// </summary>
		static AppLanguageHelper()
		{
			// Populate the Languages collection with available languages
			var appLanguages = ApplicationLanguages.ManifestLanguages
			   .Append(string.Empty) // Add default language code
			   .Select(language => new AppLanguageItem(language))
			   .OrderBy(language => language.Code is not "") // Default language on top
			   .ThenBy(language => language.Name)
			   .ToList();

			// Get the current primary language override.
			var current = new AppLanguageItem(ApplicationLanguages.PrimaryLanguageOverride);

			// Find the index of the saved language
			var index = appLanguages.IndexOf(appLanguages.FirstOrDefault(dl => dl.Name == current.Name) ?? appLanguages.First());

			// Set the system default language as the first item in the Languages collection
			var systemLanguage = new AppLanguageItem(CultureInfo.InstalledUICulture.Name, systemDefault: true);

			appLanguages[0] = appLanguages.Select(lang => lang.Name.Contains(systemLanguage.Name)).Any()
				? systemLanguage
				: new("en-US", systemDefault: true);

			// Initialize the list
			SupportedLanguages = new(appLanguages);
			PreferredLanguage = SupportedLanguages[index];
		}

		/// <summary>
		/// Attempts to change the preferred language code by index.
		/// </summary>
		/// <param name="index">The index of the new language.</param>
		/// <returns>True if the language was successfully changed; otherwise, false.</returns>
		public static bool TryChange(int index)
		{
			if (index >= SupportedLanguages.Count || PreferredLanguage == SupportedLanguages[index])
				return false;

			PreferredLanguage = SupportedLanguages[index];

			// Update the primary language override
			ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultCode : PreferredLanguage.Code;
			return true;
		}

		/// <summary>
		/// Attempts to change the preferred language code by code.
		/// </summary>
		/// <param name="code">The code of the new language.</param>
		/// <returns>True if the language was successfully changed; otherwise, false.</returns>
		public static bool TryChange(string code)
		{
			var lang = new AppLanguageItem(code);
			var find = SupportedLanguages.FirstOrDefault(dl => dl.Name == lang.Name);
			if (find is null)
				return false;

			var index = SupportedLanguages
				.Skip(1) // Skip first (default) language
				.ToList()
				.IndexOf(find ?? SupportedLanguages.First());

			// Adjusts the index to match the correct index
			index = index == 0 ? index : index + 1;

			if (PreferredLanguage == SupportedLanguages[index])
				return false;

			PreferredLanguage = SupportedLanguages[index];

			// Update the primary language override
			ApplicationLanguages.PrimaryLanguageOverride = index == 0 ? _defaultCode : PreferredLanguage.Code;
			return true;
		}

		// TODO: Replaced by RealTime Resources in the future
		/// <summary>
		/// Updates the title bar layout of the specified window based on the current culture.
		/// </summary>
		/// <param name="window">The window to be updated.</param>
		/// <returns>True if the update was successful; otherwise, false.</returns>
		public static bool UpdateTitleBar(Window window)
		{
			try
			{
				var hwnd = new HWND(WindowNative.GetWindowHandle(window));
				var exStyle = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

				exStyle = PreferredCulture.TextInfo.IsRightToLeft
					? new IntPtr((uint)exStyle | (uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL) // Set RTL layout
					: new IntPtr((uint)exStyle.ToInt64() & ~(uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL); // Set LTR layout

				if (PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle) == 0)
					return false;
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		// TODO: Replaced by RealTime Resources in the future
		public static void UpdateContextLayout(FrameworkElement element)
		{
			element.FlowDirection = PreferredCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		}

		// TODO: Replaced by RealTime Resources in the future
		public static bool SetCultureLayout(Window window)
		{
			var res = UpdateTitleBar(window);
			if (!res)
				return res;
			if (window.Content is FrameworkElement element)
				UpdateContextLayout(element);
			return res;
		}
	}
}
