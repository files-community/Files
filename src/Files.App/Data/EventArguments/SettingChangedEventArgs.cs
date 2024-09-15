// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

using System;

namespace Files.App.Data.EventArguments
{
	public sealed class SettingChangedEventArgs : EventArgs
	{
		public string SettingName { get; }

		public object? NewValue { get; }

		public SettingChangedEventArgs(string settingName, object? newValue)
		{
			SettingName = settingName;
			NewValue = newValue;
		}
	}
}
