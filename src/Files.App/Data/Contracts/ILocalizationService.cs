// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	public interface ILocalizationService
	{
		string LocalizeFromResourceKey(string resourceKey);
	}
}
