// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Files.SearchService.Index;

/// <summary>
/// Splits filenames into lowercase tokens for the inverted index.
/// Strategy: split on delimiter characters, then split each segment on
/// camelCase and digit/letter transitions.
/// e.g. "MyDocument_v2Final.docx" → ["my", "document", "v", "2", "final", "docx"]
/// </summary>
internal static class Tokenizer
{
	private static readonly SearchValues<char> Delimiters =
		SearchValues.Create([' ', '.', '_', '-', '(', ')', '[', ']', '+', '=', '&', ',']);

	/// <summary>Returns lowercase tokens for the given filename.</summary>
	public static IEnumerable<string> Tokenize(string filename)
	{
		foreach (var segment in filename.Split(
			[' ', '.', '_', '-', '(', ')', '[', ']', '+', '=', '&', ','],
			StringSplitOptions.RemoveEmptyEntries))
		{
			foreach (var token in SplitCamelCase(segment))
			{
				if (token.Length > 0)
					yield return token.ToLowerInvariant();
			}
		}
	}

	private static IEnumerable<string> SplitCamelCase(string segment)
	{
		if (segment.Length == 0) { yield break; }

		var sb = new StringBuilder();
		for (int i = 0; i < segment.Length; i++)
		{
			var c = segment[i];
			var isUpper = char.IsUpper(c);
			var isDigit = char.IsDigit(c);
			var prevIsLower = i > 0 && char.IsLower(segment[i - 1]);
			var prevIsDigit = i > 0 && char.IsDigit(segment[i - 1]);
			var nextIsLower = i + 1 < segment.Length && char.IsLower(segment[i + 1]);

			bool split =
				(isUpper && prevIsLower) ||                   // camelCase boundary
				(isUpper && nextIsLower && sb.Length > 1) ||  // acronym end: "HTMLParser"
				(isDigit && !prevIsDigit && sb.Length > 0) || // letter→digit
				(!isDigit && prevIsDigit && sb.Length > 0);   // digit→letter

			if (split && sb.Length > 0)
			{
				yield return sb.ToString();
				sb.Clear();
			}
			sb.Append(c);
		}
		if (sb.Length > 0)
			yield return sb.ToString();
	}
}
