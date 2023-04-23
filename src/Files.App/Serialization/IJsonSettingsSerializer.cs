// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Serialization
{
	internal interface IJsonSettingsSerializer
	{
		string? SerializeToJson(object? obj);

		T? DeserializeFromJson<T>(string json);
	}
}
