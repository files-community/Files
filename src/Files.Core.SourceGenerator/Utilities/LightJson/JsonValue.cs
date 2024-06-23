// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

namespace Files.Core.SourceGenerator.Utilities.LightJson
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using LightJson.Serialization;

	/// <summary>
	/// A wrapper object that contains a valid JSON value.
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}", Type = "JsonValue({Type})")]
	[DebuggerTypeProxy(typeof(JsonValueDebugView))]
	internal struct JsonValue
	{
		/// <summary>
		/// Represents a null JsonValue.
		/// </summary>
		public static readonly JsonValue Null = new JsonValue(JsonValueType.Null, default, null);

		private readonly JsonValueType type;
		private readonly object reference;
		private readonly double value;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Boolean value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(bool? value)
		{
			if (value.HasValue)
			{
				reference = null;

				type = JsonValueType.Boolean;

				this.value = value.Value ? 1 : 0;
			}
			else
			{
				this = Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(double? value)
		{
			if (value.HasValue)
			{
				reference = null;

				type = JsonValueType.Number;

				this.value = value.Value;
			}
			else
			{
				this = Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a String value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(string value)
		{
			if (value != null)
			{
				this.value = default;

				type = JsonValueType.String;

				reference = value;
			}
			else
			{
				this = Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a JsonObject.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonObject value)
		{
			if (value != null)
			{
				this.value = default;

				type = JsonValueType.Object;

				reference = value;
			}
			else
			{
				this = Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Array reference value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonArray value)
		{
			if (value != null)
			{
				this.value = default;

				type = JsonValueType.Array;

				reference = value;
			}
			else
			{
				this = Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonValue"/> struct.
		/// </summary>
		/// <param name="type">The Json type of the JsonValue.</param>
		/// <param name="value">
		/// The internal value of the JsonValue.
		/// This is used when the Json type is Number or Boolean.
		/// </param>
		/// <param name="reference">
		/// The internal value reference of the JsonValue.
		/// This value is used when the Json type is String, JsonObject, or JsonArray.
		/// </param>
		private JsonValue(JsonValueType type, double value, object reference)
		{
			this.type = type;
			this.value = value;
			this.reference = reference;
		}

		/// <summary>
		/// Gets the type of this JsonValue.
		/// </summary>
		/// <value>The type of this JsonValue.</value>
		public JsonValueType Type
		{
			get
			{
				return type;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is Null.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is Null.</value>
		public bool IsNull
		{
			get
			{
				return Type == JsonValueType.Null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a Boolean.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is a Boolean.</value>
		public bool IsBoolean
		{
			get
			{
				return Type == JsonValueType.Boolean;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is an Integer.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is an Integer.</value>
		public bool IsInteger
		{
			get
			{
				if (!IsNumber)
				{
					return false;
				}

				var value = this.value;
				return unchecked((int)value) == value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a Number.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is a Number.</value>
		public bool IsNumber
		{
			get
			{
				return Type == JsonValueType.Number;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a String.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is a String.</value>
		public bool IsString
		{
			get
			{
				return Type == JsonValueType.String;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonObject.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is a JsonObject.</value>
		public bool IsJsonObject
		{
			get
			{
				return Type == JsonValueType.Object;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonArray.
		/// </summary>
		/// <value>A value indicating whether this JsonValue is a JsonArray.</value>
		public bool IsJsonArray
		{
			get
			{
				return Type == JsonValueType.Array;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue represents a DateTime.
		/// </summary>
		/// <value>A value indicating whether this JsonValue represents a DateTime.</value>
		public bool IsDateTime
		{
			get
			{
				return AsDateTime != null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this value is true or false.
		/// </summary>
		/// <value>This value as a Boolean type.</value>
		public bool AsBoolean
		{
			get
			{
				switch (Type)
				{
					case JsonValueType.Boolean:
						return value == 1;

					case JsonValueType.Number:
						return value != 0;

					case JsonValueType.String:
						return (string)reference != string.Empty;

					case JsonValueType.Object:
					case JsonValueType.Array:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Gets this value as an Integer type.
		/// </summary>
		/// <value>This value as an Integer type.</value>
		public int AsInteger
		{
			get
			{
				var value = AsNumber;

				// Prevent overflow if the value doesn't fit.
				if (value >= int.MaxValue)
				{
					return int.MaxValue;
				}

				if (value <= int.MinValue)
				{
					return int.MinValue;
				}

				return (int)value;
			}
		}

		/// <summary>
		/// Gets this value as a Number type.
		/// </summary>
		/// <value>This value as a Number type.</value>
		public double AsNumber
		{
			get
			{
				switch (Type)
				{
					case JsonValueType.Boolean:
						return value == 1
							? 1
							: 0;

					case JsonValueType.Number:
						return value;

					case JsonValueType.String:
						double number;
						if (double.TryParse((string)reference, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
						{
							return number;
						}
						else
						{
							goto default;
						}

					default:
						return 0;
				}
			}
		}

		/// <summary>
		/// Gets this value as a String type.
		/// </summary>
		/// <value>This value as a String type.</value>
		public string AsString
		{
			get
			{
				switch (Type)
				{
					case JsonValueType.Boolean:
						return value == 1
							? "true"
							: "false";

					case JsonValueType.Number:
						return value.ToString(CultureInfo.InvariantCulture);

					case JsonValueType.String:
						return (string)reference;

					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Gets this value as an JsonObject.
		/// </summary>
		/// <value>This value as an JsonObject.</value>
		public JsonObject AsJsonObject
		{
			get
			{
				return IsJsonObject
					? (JsonObject)reference
					: null;
			}
		}

		/// <summary>
		/// Gets this value as an JsonArray.
		/// </summary>
		/// <value>This value as an JsonArray.</value>
		public JsonArray AsJsonArray
		{
			get
			{
				return IsJsonArray
					? (JsonArray)reference
					: null;
			}
		}

		/// <summary>
		/// Gets this value as a system.DateTime.
		/// </summary>
		/// <value>This value as a system.DateTime.</value>
		public DateTime? AsDateTime
		{
			get
			{
				DateTime value;

				if (IsString && DateTime.TryParse((string)reference, out value))
				{
					return value;
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets this (inner) value as a System.object.
		/// </summary>
		/// <value>This (inner) value as a System.object.</value>
		public object AsObject
		{
			get
			{
				switch (Type)
				{
					case JsonValueType.Boolean:
					case JsonValueType.Number:
						return value;

					case JsonValueType.String:
					case JsonValueType.Object:
					case JsonValueType.Array:
						return reference;

					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown when this JsonValue is not a JsonObject.
		/// </exception>
		public JsonValue this[string key]
		{
			get
			{
				if (IsJsonObject)
				{
					return ((JsonObject)reference)[key];
				}
				else
				{
					throw new InvalidOperationException("This value does not represent a JsonObject.");
				}
			}

			set
			{
				if (IsJsonObject)
				{
					((JsonObject)reference)[key] = value;
				}
				else
				{
					throw new InvalidOperationException("This value does not represent a JsonObject.");
				}
			}
		}

		/// <summary>
		/// Gets or sets the value at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown when this <see cref="JsonValue"/> is not a <see cref="JsonArray"/>.
		/// </exception>
		public JsonValue this[int index]
		{
			get
			{
				if (IsJsonArray)
				{
					return ((JsonArray)reference)[index];
				}
				else
				{
					throw new InvalidOperationException("This value does not represent a JsonArray.");
				}
			}

			set
			{
				if (IsJsonArray)
				{
					((JsonArray)reference)[index] = value;
				}
				else
				{
					throw new InvalidOperationException("This value does not represent a JsonArray.");
				}
			}
		}

		/// <summary>
		/// Converts the given nullable boolean into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(bool? value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given nullable double into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(double? value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given string into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(string value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given JsonObject into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(JsonObject value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given JsonArray into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(JsonArray value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given DateTime? into a JsonValue.
		/// </summary>
		/// <remarks>
		/// <para>The DateTime value will be stored as a string using ISO 8601 format,
		/// since JSON does not define a DateTime type.</para>
		/// </remarks>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(DateTime? value)
		{
			if (value == null)
			{
				return Null;
			}

			return new JsonValue(value.Value.ToString("o"));
		}

		/// <summary>
		/// Converts the given JsonValue into an Int.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator int(JsonValue jsonValue)
		{
			if (jsonValue.IsInteger)
			{
				return jsonValue.AsInteger;
			}
			else
			{
				return 0;
			}
		}

        /// <summary>
        /// Converts the given JsonValue into a nullable Int.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        /// <exception cref="InvalidCastException">
        /// Throws System.InvalidCastException when the inner value type of the
        /// JsonValue is not the desired type of the conversion.
        /// </exception>
#pragma warning disable IDE0055 // Fix formatting
        public static explicit operator int?(JsonValue jsonValue)
#pragma warning restore IDE0055 // Fix formatting
        {
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (int)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a Bool.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator bool(JsonValue jsonValue)
		{
			if (jsonValue.IsBoolean)
			{
				return jsonValue.value == 1;
			}
			else
			{
				return false;
			}
		}

        /// <summary>
        /// Converts the given JsonValue into a nullable Bool.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        /// <exception cref="InvalidCastException">
        /// Throws System.InvalidCastException when the inner value type of the
        /// JsonValue is not the desired type of the conversion.
        /// </exception>
#pragma warning disable IDE0055 // Fix formatting
        public static explicit operator bool?(JsonValue jsonValue)
#pragma warning restore IDE0055 // Fix formatting
        {
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (bool)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a Double.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator double(JsonValue jsonValue)
		{
			if (jsonValue.IsNumber)
			{
				return jsonValue.value;
			}
			else
			{
				return double.NaN;
			}
		}

        /// <summary>
        /// Converts the given JsonValue into a nullable Double.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        /// <exception cref="InvalidCastException">
        /// Throws System.InvalidCastException when the inner value type of the
        /// JsonValue is not the desired type of the conversion.
        /// </exception>
#pragma warning disable IDE0055 // Fix formatting
        public static explicit operator double?(JsonValue jsonValue)
#pragma warning restore IDE0055 // Fix formatting
        {
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (double)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a String.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator string(JsonValue jsonValue)
		{
			if (jsonValue.IsString || jsonValue.IsNull)
			{
				return jsonValue.reference as string;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a JsonObject.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator JsonObject(JsonValue jsonValue)
		{
			if (jsonValue.IsJsonObject || jsonValue.IsNull)
			{
				return jsonValue.reference as JsonObject;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a JsonArray.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator JsonArray(JsonValue jsonValue)
		{
			if (jsonValue.IsJsonArray || jsonValue.IsNull)
			{
				return jsonValue.reference as JsonArray;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a DateTime.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static explicit operator DateTime(JsonValue jsonValue)
		{
			var dateTime = jsonValue.AsDateTime;

			if (dateTime.HasValue)
			{
				return dateTime.Value;
			}
			else
			{
				return DateTime.MinValue;
			}
		}

        /// <summary>
        /// Converts the given JsonValue into a nullable DateTime.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
#pragma warning disable IDE0055 // Fix formatting
        public static explicit operator DateTime?(JsonValue jsonValue)
#pragma warning restore IDE0055 // Fix formatting
        {
			if (jsonValue.IsDateTime || jsonValue.IsNull)
			{
				return jsonValue.AsDateTime;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns a value indicating whether the two given JsonValues are equal.
		/// </summary>
		/// <param name="a">First JsonValue to compare.</param>
		/// <param name="b">Second JsonValue to compare.</param>
		public static bool operator ==(JsonValue a, JsonValue b)
		{
			return a.Type == b.Type
				&& a.value == b.value
				&& Equals(a.reference, b.reference);
		}

		/// <summary>
		/// Returns a value indicating whether the two given JsonValues are unequal.
		/// </summary>
		/// <param name="a">First JsonValue to compare.</param>
		/// <param name="b">Second JsonValue to compare.</param>
		public static bool operator !=(JsonValue a, JsonValue b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Returns a JsonValue by parsing the given string.
		/// </summary>
		/// <param name="text">The JSON-formatted string to be parsed.</param>
		/// <returns>The <see cref="JsonValue"/> representing the parsed text.</returns>
		public static JsonValue Parse(string text)
		{
			return JsonReader.Parse(text);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return IsNull;
			}

			var jsonValue = obj as JsonValue?;
			if (jsonValue == null)
			{
				return false;
			}
			else
			{
				return this == jsonValue.Value;
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			if (IsNull)
			{
				return Type.GetHashCode();
			}
			else
			{
				return Type.GetHashCode()
					^ value.GetHashCode()
					^ EqualityComparer<object>.Default.GetHashCode(reference);
			}
		}

		private class JsonValueDebugView
		{
			private readonly JsonValue jsonValue;

			public JsonValueDebugView(JsonValue jsonValue)
			{
				this.jsonValue = jsonValue;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonObject ObjectView
			{
				get
				{
					if (jsonValue.IsJsonObject)
					{
						return (JsonObject)jsonValue.reference;
					}
					else
					{
						return null;
					}
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonArray ArrayView
			{
				get
				{
					if (jsonValue.IsJsonArray)
					{
						return (JsonArray)jsonValue.reference;
					}
					else
					{
						return null;
					}
				}
			}

			public JsonValueType Type
			{
				get
				{
					return jsonValue.Type;
				}
			}

			public object Value
			{
				get
				{
					if (jsonValue.IsJsonObject)
					{
						return (JsonObject)jsonValue.reference;
					}
					else if (jsonValue.IsJsonArray)
					{
						return (JsonArray)jsonValue.reference;
					}
					else
					{
						return jsonValue;
					}
				}
			}
		}
	}
}
