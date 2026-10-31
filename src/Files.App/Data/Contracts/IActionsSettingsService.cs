// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Contracts
{
	internal interface IActionsSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// A dictionary to determine the custom hotkeys
		/// </summary>
		List<ActionWithParameterItem>? ActionsV2 { get; set; }
	}
}
