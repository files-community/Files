// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
	{
		public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
		{
			WriteIndented = true,
			NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
		};

		public string? SerializeToJson(object? obj)
		{
			return JsonSerializer.Serialize(obj, Options);
		}

		public T? DeserializeFromJson<T>(string json)
		{
			return JsonSerializer.Deserialize<T?>(json, Options);
		}
	}
}
