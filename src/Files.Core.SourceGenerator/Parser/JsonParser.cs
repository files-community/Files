// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.SourceGenerator.Utils.LightJson;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Files.Core.SourceGenerator.Parser
{
    internal static class JsonParser
    {
        public static IEnumerable<Tuple<string, string?>> GetKeys(AdditionalText file)
        {
            var stream = new StreamReader(file.Path).ReadToEnd();
            var json = JsonValue.Parse(stream);

            var result = new List<Tuple<string, string?>>();

            ProcessJsonObject(json, "", result);

            return result.OrderBy(k => k.Item1);
        }

        private static void ProcessJsonObject(JsonValue json, string prefix, List<Tuple<string, string?>> result)
        {
            switch (json.Type)
            {
                case JsonValueType.Object:
                    var obj = json.AsJsonObject;

                    if (obj.ContainsKey("text") && obj.ContainsKey("crowdinContext"))
                    {
                        result.Add(Tuple.Create<string, string?>(prefix, obj["crowdinContext"].AsString));
                        break;
                    }

                    foreach (var kvp in obj!)
                    {
                        string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

                        if (kvp.Value.Type == JsonValueType.Object)
                            ProcessJsonObject(kvp.Value, key, result);

                        else if (kvp.Value.Type == JsonValueType.String)
                            result.Add(Tuple.Create<string, string?>(key, null));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}