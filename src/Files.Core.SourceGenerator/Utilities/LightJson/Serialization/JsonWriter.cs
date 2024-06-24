// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

using ErrorType = Files.Core.SourceGenerator.Utilities.LightJson.Serialization.JsonSerializationException.ErrorType;

namespace Files.Core.SourceGenerator.Utilities.LightJson.Serialization
{
	/// <summary>
	/// Represents a writer that can write string representations of JsonValues.
	/// </summary>
	internal sealed class JsonWriter : IDisposable
	{
		private int indent;
		private bool isNewLine;
		private SystemIO.TextWriter writer;

		/// <summary>
		/// A set of containing all the collection objects (JsonObject/JsonArray) being rendered.
		/// It is used to prevent circular references; since collections that contain themselves
		/// will never finish rendering.
		/// </summary>
		private HashSet<IEnumerable<JsonValue>> renderingCollections;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonWriter"/> class.
		/// </summary>
		public JsonWriter()
			: this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonWriter"/> class.
		/// </summary>
		/// <param name="pretty">
		/// A value indicating whether the output of the writer should be human-readable.
		/// </param>
		public JsonWriter(bool pretty)
		{
			if (pretty)
			{
				IndentString = "\t";
				SpacingString = " ";
				NewLineString = "\n";
			}
		}

		/// <summary>
		/// Gets or sets the string representing a indent in the output.
		/// </summary>
		/// <value>
		/// The string representing a indent in the output.
		/// </value>
		public string IndentString { get; set; }

		/// <summary>
		/// Gets or sets the string representing a space in the output.
		/// </summary>
		/// <value>
		/// The string representing a space in the output.
		/// </value>
		public string SpacingString { get; set; }

		/// <summary>
		/// Gets or sets the string representing a new line on the output.
		/// </summary>
		/// <value>
		/// The string representing a new line on the output.
		/// </value>
		public string NewLineString { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether JsonObject properties should be written in a deterministic order.
		/// </summary>
		/// <value>
		/// A value indicating whether JsonObject properties should be written in a deterministic order.
		/// </value>
		public bool SortObjects { get; set; }

		/// <summary>
		/// Returns a string representation of the given JsonValue.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to serialize.</param>
		/// <returns>The serialized value.</returns>
		public string Serialize(JsonValue jsonValue)
		{
			Initialize();

			Render(jsonValue);

			return writer.ToString();
		}

		/// <summary>
		/// Releases all the resources used by this object.
		/// </summary>
		public void Dispose() => writer?.Dispose();

		private void Initialize()
		{
			indent = 0;
			isNewLine = true;
			writer = new SystemIO.StringWriter();
			renderingCollections = [];
		}

		private void Write(string text)
		{
			if (isNewLine)
			{
				isNewLine = false;
				WriteIndentation();
			}

			writer.Write(text);
		}

		private void WriteEncodedJsonValue(JsonValue value)
		{
			switch (value.Type)
			{
				case JsonValueType.Null:
					Write("null");
					break;

				case JsonValueType.Boolean:
					Write(value.AsString);
					break;

				case JsonValueType.Number:
					Write(((double)value).ToString(CultureInfo.InvariantCulture));
					break;

				default:
					Debug.Assert(value.Type == JsonValueType.String, "value.Type == JsonValueType.String");
					WriteEncodedString((string)value);
					break;
			}
		}

		private void WriteEncodedString(string text)
		{
			Write("\"");

			for (var i = 0; i < text.Length; i += 1)
			{
				var currentChar = text[i];

				// Encoding special characters.
				switch (currentChar)
				{
					case '\\':
						writer.Write("\\\\");
						break;

					case '\"':
						writer.Write("\\\"");
						break;

					case '/':
						writer.Write("\\/");
						break;

					case '\b':
						writer.Write("\\b");
						break;

					case '\f':
						writer.Write("\\f");
						break;

					case '\n':
						writer.Write("\\n");
						break;

					case '\r':
						writer.Write("\\r");
						break;

					case '\t':
						writer.Write("\\t");
						break;

					default:
						writer.Write(currentChar);
						break;
				}
			}

			writer.Write("\"");
		}

		private void WriteIndentation()
		{
			for (var i = 0; i < indent; i += 1)
			{
				Write(IndentString);
			}
		}

		private void WriteSpacing() => Write(SpacingString);

		private void WriteLine()
		{
			Write(NewLineString);
			isNewLine = true;
		}

		private void WriteLine(string line)
		{
			Write(line);
			WriteLine();
		}

		private void AddRenderingCollection(IEnumerable<JsonValue> value)
		{
			if (!renderingCollections.Add(value))
			{
				throw new JsonSerializationException(ErrorType.CircularReference);
			}
		}

		private void RemoveRenderingCollection(IEnumerable<JsonValue> value) => renderingCollections.Remove(value);

		private void Render(JsonValue value)
		{
			switch (value.Type)
			{
				case JsonValueType.Null:
				case JsonValueType.Boolean:
				case JsonValueType.Number:
				case JsonValueType.String:
					WriteEncodedJsonValue(value);
					break;

				case JsonValueType.Object:
					Render((JsonObject)value);
					break;

				case JsonValueType.Array:
					Render((JsonArray)value);
					break;

				default:
					throw new JsonSerializationException(ErrorType.InvalidValueType);
			}
		}

		private void Render(JsonArray value)
		{
			AddRenderingCollection(value);

			WriteLine("[");

			indent += 1;

			using (var enumerator = value.GetEnumerator())
			{
				var hasNext = enumerator.MoveNext();

				while (hasNext)
				{
					Render(enumerator.Current);

					hasNext = enumerator.MoveNext();

					if (hasNext)
					{
						WriteLine(",");
					}
					else
					{
						WriteLine();
					}
				}
			}

			indent -= 1;

			Write("]");

			RemoveRenderingCollection(value);
		}

		private void Render(JsonObject value)
		{
			AddRenderingCollection(value);

			WriteLine("{");

			indent += 1;

			using (var enumerator = GetJsonObjectEnumerator(value))
			{
				var hasNext = enumerator.MoveNext();

				while (hasNext)
				{
					WriteEncodedString(enumerator.Current.Key);
					Write(":");
					WriteSpacing();
					Render(enumerator.Current.Value);

					hasNext = enumerator.MoveNext();

					if (hasNext)
					{
						WriteLine(",");
					}
					else
					{
						WriteLine();
					}
				}
			}

			indent -= 1;

			Write("}");

			RemoveRenderingCollection(value);
		}

		/// <summary>
		/// Gets an JsonObject enumerator based on the configuration of this JsonWriter.
		/// If JsonWriter.SortObjects is set to true, then a ordered enumerator is returned.
		/// Otherwise, a faster non-deterministic enumerator is returned.
		/// </summary>
		/// <param name="jsonObject">The JsonObject for which to get an enumerator.</param>
		/// <returns>An enumerator for the properties in a <see cref="JsonObject"/>.</returns>
		private IEnumerator<KeyValuePair<string, JsonValue>> GetJsonObjectEnumerator(JsonObject jsonObject)
		{
			if (SortObjects)
			{
				var sortedDictionary = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);

				foreach (var item in jsonObject)
				{
					sortedDictionary.Add(item.Key, item.Value);
				}

				return sortedDictionary.GetEnumerator();
			}
			else
			{
				return jsonObject.GetEnumerator();
			}
		}
	}
}
