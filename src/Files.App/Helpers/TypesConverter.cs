// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Files.App.Helpers
{
	public class KnownTypesConverter
	{
		public JsonSerializerOptions Options { get; } = new JsonSerializerOptions();

		public KnownTypesConverter()
		{
			Options.Converters.Add(new TypesConverter());
		}
	}

	public class TypesConverter : JsonConverter<Type>
	{
		public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => typeToConvert;
		public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) => writer.WriteStringValue(value.FullName);
	}
}
