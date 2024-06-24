// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.SourceGenerator.Utilities.LightJson;

namespace Files.Core.SourceGenerator.Parser
{
	internal static class JsonParser
	{
		public static IEnumerable<Tuple<string, string?>> GetKeys(AdditionalText file)
		{
			var jsonText = new SystemIO.StreamReader(file.Path).ReadToEnd();
			var json = JsonValue.Parse(jsonText);
			var result = new List<Tuple<string, string?>>();
			ProcessJsonObject(json, string.Empty, result);
			return result.OrderBy(k => k.Item1);
		}

		private static void ProcessJsonObject(JsonValue json, string prefix, List<Tuple<string, string?>> result)
		{
			if (json.Type == JsonValueType.Object)
			{
				var obj = json.AsJsonObject;

				if (obj.ContainsKey("text") && obj.ContainsKey("crowdinContext"))
				{
					result.Add(Tuple.Create(prefix, (string?)obj["crowdinContext"].AsString));
					return;
				}

				foreach (var kvp in obj)
				{
					var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}_{kvp.Key}";

					if (kvp.Value.Type == JsonValueType.Object)
					{
						ProcessJsonObject(kvp.Value, key, result);
						continue;
					}

					if (kvp.Value.Type == JsonValueType.String)
					{
						result.Add(Tuple.Create(key, (string?)null));
						continue;
					}
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException($"Type '{json.Type}' is not supported in {nameof(JsonParser)}.");
			}
		}
	}

}