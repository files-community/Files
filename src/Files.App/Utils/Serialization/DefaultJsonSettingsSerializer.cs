// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json;

namespace Files.App.Utils.Serialization
{
	internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
	{
		public static readonly JsonSerializerOptions Options = new()
		{
			WriteIndented = true
		};

		public string? SerializeToJson(object? obj)
		{
			return JsonSerializer.Serialize(obj, Options);
		}

		public T? DeserializeFromJson<T>(string json)
		{
			return JsonSerializer.Deserialize<T?>(json);
		}
	}
}
