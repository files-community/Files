// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		int DefaultGridViewSize { get; set; }
	}
}
