// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json.Serialization.Metadata;

namespace Files.App.Utils.Serialization
{
	internal interface IJsonSettingsSerializer
	{
		string SerializeToJson<T>(T? obj, JsonTypeInfo<T?> typeInfo);

		JsonElement SerializeToElement<T>(T? obj, JsonTypeInfo<T?> typeInfo);

		T? DeserializeFromJson<T>(string json, JsonTypeInfo<T?> typeInfo);

		T? DeserializeFromElement<T>(JsonElement element, JsonTypeInfo<T?> typeInfo);
	}
}
