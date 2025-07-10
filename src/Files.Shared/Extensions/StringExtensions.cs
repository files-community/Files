// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.Shared.Extensions
{
	public static class StringExtensions
	{
		/// <summary>Gets the leftmost <paramref name="length" /> characters from a string.</summary>
		/// <param name="value">The string to retrieve the substring from.</param>
		/// <param name="length">The number of characters to retrieve.</param>
		/// <returns>The substring.</returns>
		public static string Left(this string value, int length)
		{
			ArgumentNullException.ThrowIfNull(value);
			ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

			return length > value.Length ? value : value.Substring(0, length);
		}

		/// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
		/// <param name="value">The string to retrieve the substring from.</param>
		/// <param name="length">The number of characters to retrieve.</param>
		/// <returns>The substring.</returns>
		public static string Right(this string value, int length)
		{
			ArgumentNullException.ThrowIfNull(value);
			ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

			return length > value.Length ? value : value[^length..];
		}
	}
}
