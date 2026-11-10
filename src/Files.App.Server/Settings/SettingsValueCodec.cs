// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Files.App.Server;

internal static class SettingsValueCodec
{
	internal static SettingsValue[] Encode(JsonElement value)
	{
		var nodes = new List<SettingsValue>();
		Append(nodes, string.Empty, value);
		return nodes.ToArray();
	}

	internal static SettingsValue[] EncodeObject(IDictionary<string, JsonElement> values)
	{
		var nodes = new List<SettingsValue>
		{
			new() { Kind = SettingsValueKind.Object, Name = string.Empty, Text = string.Empty, Children = values.Count },
		};
		foreach (var pair in values)
			Append(nodes, pair.Key, pair.Value);
		return nodes.ToArray();
	}

	private static void Append(List<SettingsValue> nodes, string name, JsonElement value)
	{
		var node = new SettingsValue { Name = name, Text = string.Empty };
		switch (value.ValueKind)
		{
			case JsonValueKind.Object:
				node.Kind = SettingsValueKind.Object;
				node.Children = value.EnumerateObject().Count();
				nodes.Add(node);
				foreach (var property in value.EnumerateObject())
					Append(nodes, property.Name, property.Value);
				return;
			case JsonValueKind.Array:
				node.Kind = SettingsValueKind.Array;
				node.Children = value.GetArrayLength();
				nodes.Add(node);
				foreach (var item in value.EnumerateArray())
					Append(nodes, string.Empty, item);
				return;
			case JsonValueKind.String:
				node.Kind = SettingsValueKind.String;
				node.Text = value.GetString() ?? string.Empty;
				break;
			case JsonValueKind.True:
				node.Kind = SettingsValueKind.True;
				break;
			case JsonValueKind.False:
				node.Kind = SettingsValueKind.False;
				break;
			case JsonValueKind.Number:
				if (value.TryGetInt64(out node.Signed))
					node.Kind = SettingsValueKind.Int64;
				else if (value.TryGetUInt64(out node.Unsigned))
					node.Kind = SettingsValueKind.UInt64;
				else
				{
					node.Kind = SettingsValueKind.Double;
					node.Number = value.GetDouble();
				}
				break;
		}
		nodes.Add(node);
	}

	internal static JsonElement Decode(SettingsValue[] nodes)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			var index = 0;
			Write(writer, nodes, ref index, 0);
			if (index != nodes.Length)
				throw new ArgumentException("Settings value contains trailing nodes.", nameof(nodes));
		}
		using var document = JsonDocument.Parse(stream.ToArray());
		return document.RootElement.Clone();
	}

	internal static Dictionary<string, JsonElement> DecodeObject(SettingsValue[] nodes)
	{
		var value = Decode(nodes);
		if (value.ValueKind != JsonValueKind.Object)
			throw new ArgumentException("Settings must be an object.", nameof(nodes));
		return value.EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
	}

	private static void Write(Utf8JsonWriter writer, SettingsValue[] nodes, ref int index, int depth)
	{
		if (index >= nodes.Length || depth > 64)
			throw new ArgumentException("Invalid settings value tree.", nameof(nodes));
		var node = nodes[index++];
		if (node.Children < 0 || node.Children > nodes.Length - index)
			throw new ArgumentException("Invalid settings child count.", nameof(nodes));
		if (node.Children != 0 && node.Kind is not (SettingsValueKind.Object or SettingsValueKind.Array))
			throw new ArgumentException("A scalar setting cannot contain children.", nameof(nodes));

		switch (node.Kind)
		{
			case SettingsValueKind.Object:
			case SettingsValueKind.Array:
				var isObject = node.Kind == SettingsValueKind.Object;
				if (isObject)
					writer.WriteStartObject();
				else
					writer.WriteStartArray();
				for (var child = 0; child < node.Children; child++)
				{
					if (index >= nodes.Length)
						throw new ArgumentException("Missing settings child.", nameof(nodes));
					if (isObject)
						writer.WritePropertyName(nodes[index].Name);
					Write(writer, nodes, ref index, depth + 1);
				}
				if (isObject)
					writer.WriteEndObject();
				else
					writer.WriteEndArray();
				break;
			case SettingsValueKind.String:
				writer.WriteStringValue(node.Text);
				break;
			case SettingsValueKind.True:
			case SettingsValueKind.False:
				writer.WriteBooleanValue(node.Kind == SettingsValueKind.True);
				break;
			case SettingsValueKind.Int64:
				writer.WriteNumberValue(node.Signed);
				break;
			case SettingsValueKind.UInt64:
				writer.WriteNumberValue(node.Unsigned);
				break;
			case SettingsValueKind.Double:
				writer.WriteNumberValue(node.Number);
				break;
			case SettingsValueKind.Null:
				writer.WriteNullValue();
				break;
			default:
				throw new ArgumentException("Unknown settings value kind.", nameof(nodes));
		}
	}
}
