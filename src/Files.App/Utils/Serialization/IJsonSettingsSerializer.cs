// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Serialization
{
	internal interface IJsonSettingsSerializer
	{
		string? SerializeToJson(object? obj);

		T? DeserializeFromJson<T>(string json);
	}
}
