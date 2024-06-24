// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Files.Core.SourceGenerator.Utilities.LightJson.Serialization;
using System.Diagnostics;
using System.Globalization;

#nullable disable

namespace Files.Core.SourceGenerator.Utilities.LightJson
{
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
        public static readonly JsonValue Null = new(JsonValueType.Null, default, null);

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
        /// The public  value of the JsonValue.
        /// This is used when the Json type is Number or Boolean.
        /// </param>
        /// <param name="reference">
        /// The public  value reference of the JsonValue.
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
        public JsonValueType Type => type;

        /// <summary>
        /// Gets a value indicating whether this JsonValue is Null.
        /// </summary>
        /// <value>A value indicating whether this JsonValue is Null.</value>
        public bool IsNull => Type == JsonValueType.Null;

        /// <summary>
        /// Gets a value indicating whether this JsonValue is a Boolean.
        /// </summary>
        /// <value>A value indicating whether this JsonValue is a Boolean.</value>
        public bool IsBoolean => Type == JsonValueType.Boolean;

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
        public bool IsNumber => Type == JsonValueType.Number;

        /// <summary>
        /// Gets a value indicating whether this JsonValue is a String.
        /// </summary>
        /// <value>A value indicating whether this JsonValue is a String.</value>
        public bool IsString => Type == JsonValueType.String;

        /// <summary>
        /// Gets a value indicating whether this JsonValue is a JsonObject.
        /// </summary>
        /// <value>A value indicating whether this JsonValue is a JsonObject.</value>
        public bool IsJsonObject => Type == JsonValueType.Object;

        /// <summary>
        /// Gets a value indicating whether this JsonValue is a JsonArray.
        /// </summary>
        /// <value>A value indicating whether this JsonValue is a JsonArray.</value>
        public bool IsJsonArray => Type == JsonValueType.Array;

        /// <summary>
        /// Gets a value indicating whether this JsonValue represents a DateTime.
        /// </summary>
        /// <value>A value indicating whether this JsonValue represents a DateTime.</value>
        public bool IsDateTime => AsDateTime != null;

        /// <summary>
        /// Gets a value indicating whether this value is true or false.
        /// </summary>
        /// <value>This value as a Boolean type.</value>
        public bool AsBoolean
        {
            get
            {
                return Type switch
                {
                    JsonValueType.Boolean => value == 1,
                    JsonValueType.Number => value != 0,
                    JsonValueType.String => (string)reference != string.Empty,
                    JsonValueType.Object or JsonValueType.Array => true,
                    _ => false,
                };
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

                return value <= int.MinValue ? int.MinValue : (int)value;
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
                return Type switch
                {
                    JsonValueType.Boolean => value == 1
                                                ? "true"
                                                : "false",
                    JsonValueType.Number => value.ToString(CultureInfo.InvariantCulture),
                    JsonValueType.String => (string)reference,
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Gets this value as an JsonObject.
        /// </summary>
        /// <value>This value as an JsonObject.</value>
        public JsonObject AsJsonObject => IsJsonObject
                    ? (JsonObject)reference
                    : null;

        /// <summary>
        /// Gets this value as an JsonArray.
        /// </summary>
        /// <value>This value as an JsonArray.</value>
        public JsonArray AsJsonArray => IsJsonArray
                    ? (JsonArray)reference
                    : null;

        /// <summary>
        /// Gets this value as a system.DateTime.
        /// </summary>
        /// <value>This value as a system.DateTime.</value>
        public DateTime? AsDateTime
        {
            get
            {

                return IsString && DateTime.TryParse((string)reference, out var value) ? value : null;
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
                return Type switch
                {
                    JsonValueType.Boolean or JsonValueType.Number => value,
                    JsonValueType.String or JsonValueType.Object or JsonValueType.Array => reference,
                    _ => null,
                };
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
            get => IsJsonObject
                    ? ((JsonObject)reference)[key]
                    : throw new InvalidOperationException("This value does not represent a JsonObject.");

            set => ((JsonObject)reference)[key] = IsJsonObject ? value : throw new InvalidOperationException("This value does not represent a JsonObject.");
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
            get => IsJsonArray
                    ? ((JsonArray)reference)[index]
                    : throw new InvalidOperationException("This value does not represent a JsonArray.");

            set => ((JsonArray)reference)[index] = IsJsonArray ? value : throw new InvalidOperationException("This value does not represent a JsonArray.");
        }

        /// <summary>
        /// Converts the given nullable boolean into a JsonValue.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(bool? value) => new(value);

        /// <summary>
        /// Converts the given nullable double into a JsonValue.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(double? value) => new(value);

        /// <summary>
        /// Converts the given string into a JsonValue.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(string value) => new(value);

        /// <summary>
        /// Converts the given JsonObject into a JsonValue.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(JsonObject value) => new(value);

        /// <summary>
        /// Converts the given JsonArray into a JsonValue.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(JsonArray value) => new(value);

        /// <summary>
        /// Converts the given DateTime? into a JsonValue.
        /// </summary>
        /// <remarks>
        /// <para>The DateTime value will be stored as a string using ISO 8601 format,
        /// since JSON does not define a DateTime type.</para>
        /// </remarks>
        /// <param name="value">The value to be converted.</param>
        public static implicit operator JsonValue(DateTime? value) => value == null ? Null : new JsonValue(value.Value.ToString("o"));

        /// <summary>
        /// Converts the given JsonValue into an Int.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator int(JsonValue jsonValue) => jsonValue.IsInteger ? jsonValue.AsInteger : 0;

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
=>jsonValue.IsNull ? null : (int)jsonValue;

        /// <summary>
        /// Converts the given JsonValue into a Bool.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator bool(JsonValue jsonValue) => jsonValue.IsBoolean && jsonValue.value == 1;

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
=>jsonValue.IsNull ? null : (bool)jsonValue;

        /// <summary>
        /// Converts the given JsonValue into a Double.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator double(JsonValue jsonValue) => jsonValue.IsNumber ? jsonValue.value : double.NaN;

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
=>jsonValue.IsNull ? null : (double)jsonValue;

        /// <summary>
        /// Converts the given JsonValue into a String.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator string(JsonValue jsonValue) => jsonValue.IsString || jsonValue.IsNull ? jsonValue.reference as string : null;

        /// <summary>
        /// Converts the given JsonValue into a JsonObject.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator JsonObject(JsonValue jsonValue) => jsonValue.IsJsonObject || jsonValue.IsNull ? jsonValue.reference as JsonObject : null;

        /// <summary>
        /// Converts the given JsonValue into a JsonArray.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator JsonArray(JsonValue jsonValue) => jsonValue.IsJsonArray || jsonValue.IsNull ? jsonValue.reference as JsonArray : null;

        /// <summary>
        /// Converts the given JsonValue into a DateTime.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
        public static explicit operator DateTime(JsonValue jsonValue)
        {
            var dateTime = jsonValue.AsDateTime;

            return dateTime.HasValue ? dateTime.Value : DateTime.MinValue;
        }

        /// <summary>
        /// Converts the given JsonValue into a nullable DateTime.
        /// </summary>
        /// <param name="jsonValue">The JsonValue to be converted.</param>
#pragma warning disable IDE0055 // Fix formatting
        public static explicit operator DateTime?(JsonValue jsonValue)
=>jsonValue.IsDateTime || jsonValue.IsNull ? jsonValue.AsDateTime : null;

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
        public static bool operator !=(JsonValue a, JsonValue b) => !(a == b);

        /// <summary>
        /// Returns a JsonValue by parsing the given string.
        /// </summary>
        /// <param name="text">The JSON-formatted string to be parsed.</param>
        /// <returns>The <see cref="JsonValue"/> representing the parsed text.</returns>
        public static JsonValue Parse(string text) => JsonReader.Parse(text);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return IsNull;
            }

            var jsonValue = obj as JsonValue?;
            return jsonValue != null && this == jsonValue.Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return IsNull
                ? Type.GetHashCode()
                : Type.GetHashCode()
                    ^ value.GetHashCode()
                    ^ EqualityComparer<object>.Default.GetHashCode(reference);
        }

        private class JsonValueDebugView
        {
            private readonly JsonValue jsonValue;

            public JsonValueDebugView(JsonValue jsonValue) => this.jsonValue = jsonValue;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public JsonObject ObjectView => jsonValue.IsJsonObject ? (JsonObject)jsonValue.reference : null;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public JsonArray ArrayView => jsonValue.IsJsonArray ? (JsonArray)jsonValue.reference : null;

            public JsonValueType Type => jsonValue.Type;

            public  object Value
            {
                get
                {
                    if (jsonValue.IsJsonObject)
                    {
                        return (JsonObject)jsonValue.reference;
                    }
                    else
                    {
                        return jsonValue.IsJsonArray ? (JsonArray)jsonValue.reference : (object)jsonValue;
                    }
                }
            }
        }
    }
}
