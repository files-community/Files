// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Files.Backend.Services;

namespace Files.App.Services
{
	internal sealed class LocalizationService : ILocalizationService
	{
		public string LocalizeFromResourceKey(string resourceKey)
		{
			return resourceKey.GetLocalizedResource();
		}
	}
}
