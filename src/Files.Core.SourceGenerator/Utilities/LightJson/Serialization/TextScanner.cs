// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable disable

using ErrorType = Files.Core.SourceGenerator.Utilities.LightJson.Serialization.JsonParseException.ErrorType;

namespace Files.Core.SourceGenerator.Utilities.LightJson.Serialization
{
	/// <summary>
	/// Represents a text scanner that reads one character at a time.
	/// </summary>
	internal sealed class TextScanner
	{
		private readonly SystemIO.TextReader reader;
		private TextPosition position;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextScanner"/> class.
		/// </summary>
		/// <param name="reader">The TextReader to read the text.</param>
		public TextScanner(SystemIO.TextReader reader) => this.reader = reader;

		/// <summary>
		/// Gets the position of the scanner within the text.
		/// </summary>
		/// <value>The position of the scanner within the text.</value>
		public TextPosition Position => position;

		/// <summary>
		/// Reads the next character in the stream without changing the current position.
		/// </summary>
		/// <returns>The next character in the stream.</returns>
		public char Peek()
			=> (char)Peek(throwAtEndOfFile: true);

		/// <summary>
		/// Reads the next character in the stream without changing the current position.
		/// </summary>
		/// <param name="throwAtEndOfFile"><see langword="true"/> to throw an exception if the end of the file is
		/// reached; otherwise, <see langword="false"/>.</param>
		/// <returns>The next character in the stream, or -1 if the end of the file is reached with
		/// <paramref name="throwAtEndOfFile"/> set to <see langword="false"/>.</returns>
		public int Peek(bool throwAtEndOfFile)
		{
			var next = reader.Peek();

			return next == -1 && throwAtEndOfFile
				? throw new JsonParseException(
					ErrorType.IncompleteMessage,
					position)
				: next;
		}

		/// <summary>
		/// Reads the next character in the stream, advancing the text position.
		/// </summary>
		/// <returns>The next character in the stream.</returns>
		public char Read()
		{
			var next = reader.Read();

			if (next == -1)
			{
				throw new JsonParseException(
					ErrorType.IncompleteMessage,
					position);
			}
			else
			{
				if (next == '\n')
				{
					position.Line += 1;
					position.Column = 0;
				}
				else
				{
					position.Column += 1;
				}

				return (char)next;
			}
		}

		/// <summary>
		/// Advances the scanner to next non-whitespace character.
		/// </summary>
		public void SkipWhitespace()
		{
			while (true)
			{
				var next = Peek();
				if (char.IsWhiteSpace(next))
				{
					_ = Read();
					continue;
				}
				else if (next == '/')
				{
					SkipComment();
					continue;
				}
				else
				{
					break;
				}
			}
		}

		/// <summary>
		/// Verifies that the given character matches the next character in the stream.
		/// If the characters do not match, an exception will be thrown.
		/// </summary>
		/// <param name="next">The expected character.</param>
		public void Assert(char next)
		{
			var errorPosition = position;
			if (Read() != next)
			{
				throw new JsonParseException(
					string.Format("Parser expected '{0}'", next),
					ErrorType.InvalidOrUnexpectedCharacter,
					errorPosition);
			}
		}

		/// <summary>
		/// Verifies that the given string matches the next characters in the stream.
		/// If the strings do not match, an exception will be thrown.
		/// </summary>
		/// <param name="next">The expected string.</param>
		public void Assert(string next)
		{
			for (var i = 0; i < next.Length; i += 1)
			{
				Assert(next[i]);
			}
		}

		private void SkipComment()
		{
			// First character is the first slash
			_ = Read();
			switch (Peek())
			{
				case '/':
					SkipLineComment();
					return;

				case '*':
					SkipBlockComment();
					return;

				default:
					throw new JsonParseException(
						string.Format("Parser expected '{0}'", Peek()),
						ErrorType.InvalidOrUnexpectedCharacter,
						position);
			}
		}

		private void SkipLineComment()
		{
			// First character is the second '/' of the opening '//'
			_ = Read();

			while (true)
			{
				switch (reader.Peek())
				{
					case '\n':
						// Reached the end of the line
						_ = Read();
						return;

					case -1:
						// Reached the end of the file
						return;

					default:
						_ = Read();
						continue;
				}
			}
		}

		private void SkipBlockComment()
		{
			// First character is the '*' of the opening '/*'
			_ = Read();

			var foundStar = false;
			while (true)
			{
				switch (reader.Peek())
				{
					case '*':
						_ = Read();
						foundStar = true;
						continue;

					case '/':
						_ = Read();
						if (foundStar)
						{
							return;
						}
						else
						{
							foundStar = false;
							continue;
						}

					case -1:
						// Reached the end of the file
						return;

					default:
						_ = Read();
						foundStar = false;
						continue;
				}
			}
		}
	}
}
