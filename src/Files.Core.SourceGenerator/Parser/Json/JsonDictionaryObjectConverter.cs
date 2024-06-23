// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;

namespace Files.Core.SourceGenerator.Parser.Json
{
    internal class DictionaryObjectConverter : JsonConverter<IDictionary<string, object?>>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IDictionary<string, object?>);

        public override IDictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType != JsonTokenType.StartObject ? throw new JsonException() : ReadDictionary(ref reader, options);

        private IDictionary<string, object?> ReadDictionary(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<string, object?>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propertyName = reader.GetString()!;
                _ = reader.Read();

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var nestedDict = ReadDictionary(ref reader, options);
                    dictionary[propertyName] = nestedDict.TryGetValue("text", out var val) && nestedDict.ContainsKey("crowdinContext")
                        ? val : nestedDict;

                    continue;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    dictionary[propertyName] = reader.GetString();
                    continue;
                }

                var value = JsonSerializer.Deserialize<object?>(ref reader, options);
                if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var nestedDict = element.ToObject() as IDictionary<string, object?>;
                    dictionary[propertyName] = nestedDict != null &&
                        nestedDict.TryGetValue("text", out var val) && nestedDict.ContainsKey("crowdinContext")
                        ? val : nestedDict;

                    continue;
                }

                dictionary[propertyName] = value;
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, IDictionary<string, object?> value, JsonSerializerOptions options) { }
    }
}