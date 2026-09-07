// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json.Serialization.Metadata;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
	{
		public string SerializeToJson<T>(T? obj, JsonTypeInfo<T?> typeInfo)
		{
			return JsonSerializer.Serialize(obj, typeInfo);
		}

		public JsonElement SerializeToElement<T>(T? obj, JsonTypeInfo<T?> typeInfo)
		{
			return JsonSerializer.SerializeToElement(obj, typeInfo);
		}

		public T? DeserializeFromJson<T>(string json, JsonTypeInfo<T?> typeInfo)
		{
			return JsonSerializer.Deserialize(json, typeInfo);
		}

		public T? DeserializeFromElement<T>(JsonElement element, JsonTypeInfo<T?> typeInfo)
		{
			return element.Deserialize(typeInfo);
		}
	}
}
