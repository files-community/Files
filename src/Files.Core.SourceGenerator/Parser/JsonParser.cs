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
			if (json.Type is not JsonValueType.Object)
				return;

			var obj = json.AsJsonObject;

			if (obj.ContainsKey("text") && obj.ContainsKey("crowdinContext"))
			{
				if (string.IsNullOrEmpty(prefix))
					return;

				result.Add(Tuple.Create(prefix, (string?)obj["crowdinContext"].AsString));
				return;
			}

			foreach (var kvp in obj)
			{
				var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}_{kvp.Key}";

				switch (kvp.Value.Type)
				{
					case JsonValueType.Boolean:
					case JsonValueType.Number:
					case JsonValueType.String:
						result.Add(Tuple.Create(key, (string?)null));
						break;

					case JsonValueType.Object:
						ProcessJsonObject(kvp.Value, key, result);
						break;

					default:
						break;
				}
			}
		}
	}
}