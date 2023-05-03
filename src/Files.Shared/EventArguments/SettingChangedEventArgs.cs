// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Shared.EventArguments
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
