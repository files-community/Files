// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.SourceGenerator.Utilities.LightJson;

namespace Files.Core.SourceGenerator.Parser
{
	/// <summary>
	/// Provides methods to parse JSON files and extract keys with optional context information.
	/// </summary>
	internal static class JsonParser
	{
		/// <summary>
		/// Retrieves all keys and optional context information from the specified JSON file.
		/// </summary>
		/// <param name="file">The additional text representing the JSON file.</param>
		/// <returns>An enumerable of tuples where each tuple contains a key and its associated context.</returns>
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
				var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

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
