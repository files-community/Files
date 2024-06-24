// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

using System.Globalization;
using System.IO;
using ErrorType = Files.Core.SourceGenerator.Utilities.LightJson.Serialization.JsonParseException.ErrorType;

namespace Files.Core.SourceGenerator.Utilities.LightJson.Serialization
{
	/// <summary>
	/// Represents a reader that can read JsonValues.
	/// </summary>
	internal sealed class JsonReader
	{
		private readonly TextScanner scanner;

		private JsonReader(TextReader reader) => scanner = new TextScanner(reader);

		/// <summary>
		/// Creates a JsonValue by using the given TextReader.
		/// </summary>
		/// <param name="reader">The TextReader used to read a JSON message.</param>
		/// <returns>The parsed <see cref="JsonValue"/>.</returns>
		public static JsonValue Parse(TextReader reader) => reader == null ? throw new ArgumentNullException(nameof(reader)) : new JsonReader(reader).Parse();

		/// <summary>
		/// Creates a JsonValue by reader the JSON message in the given string.
		/// </summary>
		/// <param name="source">The string containing the JSON message.</param>
		/// <returns>The parsed <see cref="JsonValue"/>.</returns>
		public static JsonValue Parse(string source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			using var reader = new StringReader(source);
			return Parse(reader);
		}

		private string ReadJsonKey() => ReadString();

		private JsonValue ReadJsonValue()
		{
			scanner.SkipWhitespace();

			var next = scanner.Peek();

			return char.IsNumber(next)
				? ReadNumber()
				: next switch
				{
					'{' => (JsonValue)ReadObject(),
					'[' => (JsonValue)ReadArray(),
					'"' => (JsonValue)ReadString(),
					'-' => ReadNumber(),
					't' or 'f' => ReadBoolean(),
					'n' => ReadNull(),
					_ => throw new JsonParseException(
											ErrorType.InvalidOrUnexpectedCharacter,
											scanner.Position),
				};
		}

		private JsonValue ReadNull()
		{
			scanner.Assert("null");
			return JsonValue.Null;
		}

		private JsonValue ReadBoolean()
		{
			switch (scanner.Peek())
			{
				case 't':
					scanner.Assert("true");
					return true;

				default:
					scanner.Assert("false");
					return false;
			}
		}

		private void ReadDigits(StringBuilder builder)
		{
			while (true)
			{
				var next = scanner.Peek(throwAtEndOfFile: false);
				if (next == -1 || !char.IsNumber((char)next))
				{
					return;
				}

				_ = builder.Append(scanner.Read());
			}
		}

		private JsonValue ReadNumber()
		{
			var builder = new StringBuilder();

			if (scanner.Peek() == '-')
			{
				_ = builder.Append(scanner.Read());
			}

			if (scanner.Peek() == '0')
			{
				_ = builder.Append(scanner.Read());
			}
			else
			{
				ReadDigits(builder);
			}

			if (scanner.Peek(throwAtEndOfFile: false) == '.')
			{
				_ = builder.Append(scanner.Read());
				ReadDigits(builder);
			}

			if (scanner.Peek(throwAtEndOfFile: false) is 'e' or 'E')
			{
				_ = builder.Append(scanner.Read());

				var next = scanner.Peek();

				switch (next)
				{
					case '+':
					case '-':
						_ = builder.Append(scanner.Read());
						break;
				}

				ReadDigits(builder);
			}

			return double.Parse(
				builder.ToString(),
				CultureInfo.InvariantCulture);
		}

		private string ReadString()
		{
			var builder = new StringBuilder();

			scanner.Assert('"');

			while (true)
			{
				var errorPosition = scanner.Position;
				var c = scanner.Read();

				if (c == '\\')
				{
					errorPosition = scanner.Position;
					c = scanner.Read();

					_ = char.ToLower(c) switch
					{
						'"' or '\\' or '/' => builder.Append(c),
						'b' => builder.Append('\b'),
						'f' => builder.Append('\f'),
						'n' => builder.Append('\n'),
						'r' => builder.Append('\r'),
						't' => builder.Append('\t'),
						'u' => builder.Append(ReadUnicodeLiteral()),
						_ => throw new JsonParseException(
														ErrorType.InvalidOrUnexpectedCharacter,
														errorPosition),
					};
				}
				else if (c == '"')
				{
					break;
				}
				else
				{
					_ = char.IsControl(c)
						? throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							errorPosition)
						: builder.Append(c);
				}
			}

			return builder.ToString();
		}

		private int ReadHexDigit()
		{
			var errorPosition = scanner.Position;
			return char.ToUpper(scanner.Read()) switch
			{
				'0' => 0,
				'1' => 1,
				'2' => 2,
				'3' => 3,
				'4' => 4,
				'5' => 5,
				'6' => 6,
				'7' => 7,
				'8' => 8,
				'9' => 9,
				'A' => 10,
				'B' => 11,
				'C' => 12,
				'D' => 13,
				'E' => 14,
				'F' => 15,
				_ => throw new JsonParseException(
										ErrorType.InvalidOrUnexpectedCharacter,
										errorPosition),
			};
		}

		private char ReadUnicodeLiteral()
		{
			var value = 0;

			value += ReadHexDigit() * 4096; // 16^3
			value += ReadHexDigit() * 256;  // 16^2
			value += ReadHexDigit() * 16;   // 16^1
			value += ReadHexDigit();        // 16^0

			return (char)value;
		}

		private JsonObject ReadObject() => ReadObject([]);

		private JsonObject ReadObject(JsonObject jsonObject)
		{
			scanner.Assert('{');

			scanner.SkipWhitespace();

			if (scanner.Peek() == '}')
			{
				_ = scanner.Read();
			}
			else
			{
				while (true)
				{
					scanner.SkipWhitespace();

					var errorPosition = scanner.Position;
					var key = ReadJsonKey();

					if (jsonObject.ContainsKey(key))
					{
						throw new JsonParseException(
							ErrorType.DuplicateObjectKeys,
							errorPosition);
					}

					scanner.SkipWhitespace();

					scanner.Assert(':');

					scanner.SkipWhitespace();

					var value = ReadJsonValue();

					_ = jsonObject.Add(key, value);

					scanner.SkipWhitespace();

					errorPosition = scanner.Position;
					var next = scanner.Read();
					if (next == ',')
					{
						// Allow trailing commas in objects
						scanner.SkipWhitespace();
						if (scanner.Peek() == '}')
						{
							next = scanner.Read();
						}
					}

					if (next == '}')
					{
						break;
					}
					else if (next == ',')
					{
						continue;
					}
					else
					{
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							errorPosition);
					}
				}
			}

			return jsonObject;
		}

		private JsonArray ReadArray() => ReadArray([]);

		private JsonArray ReadArray(JsonArray jsonArray)
		{
			scanner.Assert('[');

			scanner.SkipWhitespace();

			if (scanner.Peek() == ']')
			{
				_ = scanner.Read();
			}
			else
			{
				while (true)
				{
					scanner.SkipWhitespace();

					var value = ReadJsonValue();

					_ = jsonArray.Add(value);

					scanner.SkipWhitespace();

					var errorPosition = scanner.Position;
					var next = scanner.Read();
					if (next == ',')
					{
						// Allow trailing commas in arrays
						scanner.SkipWhitespace();
						if (scanner.Peek() == ']')
						{
							next = scanner.Read();
						}
					}

					if (next == ']')
					{
						break;
					}
					else if (next == ',')
					{
						continue;
					}
					else
					{
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							errorPosition);
					}
				}
			}

			return jsonArray;
		}

		private JsonValue Parse()
		{
			scanner.SkipWhitespace();
			return ReadJsonValue();
		}
	}
}
