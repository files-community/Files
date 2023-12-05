// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		public List<DetailsLayoutColumnItemModel> Columns { get; set; }

		int DefaultGridViewSize { get; set; }
	}
}
