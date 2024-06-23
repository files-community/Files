// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Files.Core.SourceGenerator.Parser.Json
{
    internal static class JsonElementExtensions
    {
        public static object? ToObject(this JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                        dict[prop.Name] = prop.Value.ToObject();

                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                        list.Add(item.ToObject());

                    return list;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;

                    if (element.TryGetInt64(out var longValue))
                        return longValue;

                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    throw new InvalidOperationException($"Unexpected JsonValueKind {element.ValueKind}");
            }
        }
    }
}