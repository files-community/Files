// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using LightJson;

namespace Files.Core.SourceGenerator.Parser
{
	/// <summary>
	/// Provides methods to parse JSON files and extract keys with optional context information.
	/// </summary>
	internal static class JsonParser
	{
		/// <summary>
		/// Parses a JSON file and extracts keys with optional context information.
		/// </summary>
		/// <param name="file">The <see cref="AdditionalText"/> representing the JSON file to parse.</param>
		/// <returns>An <see cref="IEnumerable{ParserItem}"/> containing the extracted keys and their associated values.</returns>
		internal static IEnumerable<ParserItem> GetKeys(AdditionalText file)
		{
			var jsonText = new SystemIO.StreamReader(file.Path).ReadToEnd();
			var json = JsonValue.Parse(jsonText);
			var result = new List<ParserItem>();
			ProcessJsonObject(json, string.Empty, result);
			return result.OrderBy(item => item.Key);
		}

		private static void ProcessJsonObject(JsonValue json, string prefix, List<ParserItem> result)
		{
			if (json.Type is not JsonValueType.Object)
				return;

			var obj = json.AsJsonObject;

			if (obj.ContainsKey("text") && obj.ContainsKey("crowdinContext"))
			{
				if (string.IsNullOrEmpty(prefix))
					return;

				result.Add(new()
				{
					Key = prefix,
					Value = obj["text"],
					Comment = obj["crowdinContext"].AsString
				});

				return;
			}

			foreach (var kvp in obj)
			{
				var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

				switch (kvp.Value.Type)
				{
					case JsonValueType.Boolean:
					case JsonValueType.Number:
					case JsonValueType.String:
						result.Add(new()
						{
							Key = key,
							Value = kvp.Value.ToString()
						});
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
