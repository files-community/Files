﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Globalization;

namespace Files.App.Data.Items
{
	public class AppLanguageItem
	{
		public string LanguageID { get; set; }

		public string LanguageName { get; set; }

		public AppLanguageItem(string languagID)
		{
			if (!string.IsNullOrEmpty(languagID))
			{
				var info = new CultureInfo(languagID);
				LanguageID = info.Name;
				LanguageName = info.NativeName;
			}
			else
			{
				LanguageID = string.Empty;
				var systemDefaultLanguageOptionStr = "SettingsPreferencesSystemDefaultLanguageOption".GetLocalizedResource();

				LanguageName = string.IsNullOrEmpty(systemDefaultLanguageOptionStr) ? "System Default" : systemDefaultLanguageOptionStr;
			}
		}

		public override string ToString()
			=> LanguageName;
	}
}
