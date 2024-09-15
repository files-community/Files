// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Serialization
{
	internal interface IJsonSettingsSerializer
	{
		string? SerializeToJson(object? obj);

		T? DeserializeFromJson<T>(string json);
	}
}
