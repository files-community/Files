// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Backend.Services
{
	public interface ILocalizationService
	{
		string LocalizeFromResourceKey(string resourceKey);
	}
}