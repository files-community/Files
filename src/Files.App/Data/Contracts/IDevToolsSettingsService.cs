// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	public interface IDevToolsSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value when the Open in IDE button should be displayed on the status bar.
		/// </summary>
		OpenInIDEOption OpenInIDEOption { get; set; }
	}
}
