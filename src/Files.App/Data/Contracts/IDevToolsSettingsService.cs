// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IDevToolsSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value when the Open in IDE button should be displayed on the status bar.
		/// </summary>
		OpenInIDEOption OpenInIDEOption { get; set; }
		
		/// <summary>
		/// Gets or sets the path of the chosen IDE.
		/// </summary>
		string IDEPath { get; set; }

		/// <summary>
		/// Gets or sets the name of the chosen IDE.
		/// </summary>
		string IDEName { get; set; }
	}
}
