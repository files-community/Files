// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

namespace Files.Core.SourceGenerator.Utils.LightJson.Serialization
{
    using System;

    /// <summary>
    /// The exception that is thrown when a JSON message cannot be parsed.
    /// </summary>
    /// <remarks>
    /// <para>This exception is only intended to be thrown by LightJson.</para>
    /// </remarks>
    internal sealed class JsonParseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParseException"/> class.
        /// </summary>
        public JsonParseException()
            : base(GetDefaultMessage(ErrorType.Unknown))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParseException"/> class with the given error type and position.
        /// </summary>
        /// <param name="type">The error type that describes the cause of the error.</param>
        /// <param name="position">The position in the text where the error occurred.</param>
        public JsonParseException(ErrorType type, TextPosition position)
            : this(GetDefaultMessage(type), type, position)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParseException"/> class with the given message, error type, and position.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="type">The error type that describes the cause of the error.</param>
        /// <param name="position">The position in the text where the error occurred.</param>
        public JsonParseException(string message, ErrorType type, TextPosition position)
            : base(message)
        {
            Type = type;
            Position = position;
        }

        /// <summary>
        /// Enumerates the types of errors that can occur when parsing a JSON message.
        /// </summary>
        public enum ErrorType : int
        {
            /// <summary>
            /// Indicates that the cause of the error is unknown.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Indicates that the text ended before the message could be parsed.
            /// </summary>
            IncompleteMessage,

            /// <summary>
            /// Indicates that a JsonObject contains more than one key with the same name.
            /// </summary>
            DuplicateObjectKeys,

            /// <summary>
            /// Indicates that the parser encountered and invalid or unexpected character.
            /// </summary>
            InvalidOrUnexpectedCharacter,
        }

        /// <summary>
        /// Gets the text position where the error occurred.
        /// </summary>
        /// <value>The text position where the error occurred.</value>
        public TextPosition Position { get; private set; }

        /// <summary>
        /// Gets the type of error that caused the exception to be thrown.
        /// </summary>
        /// <value>The type of error that caused the exception to be thrown.</value>
        public ErrorType Type { get; private set; }

        private static string GetDefaultMessage(ErrorType type)
        {
            switch (type)
            {
                case ErrorType.IncompleteMessage:
                    return "The string ended before a value could be parsed.";

                case ErrorType.InvalidOrUnexpectedCharacter:
                    return "The parser encountered an invalid or unexpected character.";

                case ErrorType.DuplicateObjectKeys:
                    return "The parser encountered a JsonObject with duplicate keys.";

                default:
                    return "An error occurred while parsing the JSON message.";
            }
        }
    }
}
