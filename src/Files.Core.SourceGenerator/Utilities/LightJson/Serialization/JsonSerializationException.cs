// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

namespace Files.Core.SourceGenerator.Utilities.LightJson.Serialization
{
	/// <summary>
	/// The exception that is thrown when a JSON value cannot be serialized.
	/// </summary>
	/// <remarks>
	/// <para>This exception is only intended to be thrown by LightJson.</para>
	/// </remarks>
	internal sealed class JsonSerializationException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
		/// </summary>
		public JsonSerializationException()
			: base(GetDefaultMessage(ErrorType.Unknown))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class with the given error type.
		/// </summary>
		/// <param name="type">The error type that describes the cause of the error.</param>
		public JsonSerializationException(ErrorType type)
			: this(GetDefaultMessage(type), type)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSerializationException"/> class with the given message and
		/// error type.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="type">The error type that describes the cause of the error.</param>
		public JsonSerializationException(string message, ErrorType type)
			: base(message) => Type = type;

		/// <summary>
		/// Enumerates the types of errors that can occur during serialization.
		/// </summary>
		public enum ErrorType
		{
			/// <summary>
			/// Indicates that the cause of the error is unknown.
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// Indicates that the writer encountered an invalid number value (NAN, infinity) during serialization.
			/// </summary>
			InvalidNumber,

			/// <summary>
			/// Indicates that the object been serialized contains an invalid JSON value type.
			/// That is, a value type that is not null, boolean, number, string, object, or array.
			/// </summary>
			InvalidValueType,

			/// <summary>
			/// Indicates that the object been serialized contains a circular reference.
			/// </summary>
			CircularReference,
		}

		/// <summary>
		/// Gets the type of error that caused the exception to be thrown.
		/// </summary>
		/// <value>
		/// The type of error that caused the exception to be thrown.
		/// </value>
		public ErrorType Type { get; }

		private static string GetDefaultMessage(ErrorType type)
		{
			return type switch
			{
				ErrorType.InvalidNumber => "The value been serialized contains an invalid number value (NAN, infinity).",
				ErrorType.InvalidValueType => "The value been serialized contains (or is) an invalid JSON type.",
				ErrorType.CircularReference => "The value been serialized contains circular references.",
				_ => "An error occurred during serialization.",
			};
		}
	}
}
