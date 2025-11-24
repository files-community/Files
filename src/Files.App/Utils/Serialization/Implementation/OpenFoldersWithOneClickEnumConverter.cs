// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Files.App.Utils.Serialization.Implementation
{
	/// <summary>
	/// Custom JSON converter for OpenFoldersWithOneClickEnum that provides backward compatibility
	/// with legacy boolean values stored in settings files from previous versions.
	/// </summary>
	internal sealed class OpenFoldersWithOneClickEnumConverter : JsonConverter<OpenFoldersWithOneClickEnum>
	{
		public override OpenFoldersWithOneClickEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// Handle legacy boolean values for backward compatibility
			if (reader.TokenType == JsonTokenType.True)
			{
				// Legacy true -> Always open folders with one click
				return OpenFoldersWithOneClickEnum.Always;
			}
			else if (reader.TokenType == JsonTokenType.False)
			{
				// Legacy false -> Never open folders with one click
				return OpenFoldersWithOneClickEnum.Never;
			}
			// Handle standard enum values (number or string)
			else if (reader.TokenType == JsonTokenType.Number)
			{
				// Numeric enum value
				return (OpenFoldersWithOneClickEnum)reader.GetInt32();
			}
			else if (reader.TokenType == JsonTokenType.String)
			{
				// String enum name
				var enumString = reader.GetString();
				if (Enum.TryParse<OpenFoldersWithOneClickEnum>(enumString, ignoreCase: true, out var result))
				{
					return result;
				}
			}

			// Default fallback value
			return OpenFoldersWithOneClickEnum.OnlyInColumnsView;
		}

		public override void Write(Utf8JsonWriter writer, OpenFoldersWithOneClickEnum value, JsonSerializerOptions options)
		{
			// Write as string for better readability in settings file
			writer.WriteStringValue(value.ToString());
		}
	}
}