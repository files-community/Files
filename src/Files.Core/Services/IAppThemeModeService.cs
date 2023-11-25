// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface IAppThemeModeService
	{
		public event EventHandler? ThemeModeChanged;

		public AppThemeMode ThemeMode { get; set; }

		public void RefreshThemeMode();
	}
}
