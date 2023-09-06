// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;

namespace Files.Core.Services.Settings
{
	public interface IAppSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the StatusCenter teaching tip.
		/// </summary>
		bool ShowStatusCenterTeachingTip { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to restore tabs on startup.
		/// This is used when prompting users to restart after changing the app language.
		/// </summary>
		bool RestoreTabsOnStartup { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to force process termination regardless of background running settings.
		/// This is used when restarting the app to update.
		/// </summary>
		bool ForceProcessTermination { get; set; }
	}
}
