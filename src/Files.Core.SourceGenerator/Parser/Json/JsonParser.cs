// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Files.Core.SourceGenerator.Parser.Json
{
    internal static class JsonParser
    {
        public static IEnumerable<(string, string?)> GetKeys(AdditionalText file)
        {
            using var reader = new StreamReader(file.Path, Encoding.UTF8);

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters = { new DictionaryObjectConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var options = jsonSerializerOptions;

            var deserializeData = JsonSerializer.Deserialize<IDictionary<string, object>>(reader.BaseStream, options);
            var data = (deserializeData ?? new Dictionary<string, object>()).ToFrozenDictionary();

            if (data is null || data.Count == 0)
                return [];

            var keys = new List<string>();
            GetKeysRecursive(data, string.Empty, ref keys);
            return keys.Select<string, (string, string?)>(k => (k, null)).OrderBy(k => k);
        }

        private static void GetKeysRecursive(in IDictionary<string, object> dict, in string parentKey, ref List<string> keys)
        {
            foreach (var kvp in dict)
            {

                var key = string.IsNullOrEmpty(parentKey) ? kvp.Key : $"{parentKey}.{kvp.Key}";

                if (kvp.Value is IDictionary<string, object> nestedDict)
                    GetKeysRecursive(nestedDict, key, ref keys);
                else
                    keys.Add(key);
            }
        }
    }
}
