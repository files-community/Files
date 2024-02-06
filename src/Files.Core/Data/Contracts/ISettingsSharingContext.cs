// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	/// <summary>
	/// Represents sharing context of json settings.
	/// This enables settings classes to use the same settings file and cache for the same json file.
	/// </summary>
	public interface ISettingsSharingContext
	{
		internal BaseJsonSettings Instance { get; }
	}
}
