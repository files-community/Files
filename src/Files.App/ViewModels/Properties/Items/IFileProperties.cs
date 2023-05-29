// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Properties
{
	internal interface IFileProperties
	{
		Task GetSystemFilePropertiesAsync();

		Task SyncPropertyChangesAsync();

		Task ClearPropertiesAsync();
	}
}
