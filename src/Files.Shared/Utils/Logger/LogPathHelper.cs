// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Files.Shared
{
	public static partial class LogPathHelper
	{
		// Absolute drive ("C:\...") or UNC ("\\server\...") path token. Quotes, commas and colons end
		// the token so quoted paths in exception messages ("Could not find file 'C:\...'.") keep their
		// surrounding text; a lone "\\" followed by "?" (device IDs like "\\?\STORAGE#...") never matches.
		[GeneratedRegex(@"(?<![A-Za-z0-9_])(?:[A-Za-z]:\\|\\\\[^\\/:*?""'<>|\r\n\s]+[\\/])[^:*?,""'<>|\r\n]+")]
		private static partial Regex AbsolutePathRegex();

		// Profile folder segment in any "...\Users\<name>\..." path, either slash direction
		[GeneratedRegex(@"(?<=[\\/][Uu]sers[\\/])[^\\/:*?""'<>|\r\n]+")]
		private static partial Regex UserProfileSegmentRegex();

		// Email address token; "%40" also matches the URL-encoded "@" in remote-URL userinfo
		[GeneratedRegex(@"[A-Za-z0-9._+\-]+(?:@|%40)[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
		private static partial Regex EmailRegex();

		// Authority of a "scheme://" URL (userinfo, host, port), up to the path/query/fragment
		[GeneratedRegex(@"(?<=://)[^/?#\s""'<>|\\,;)\]]+")]
		private static partial Regex UrlAuthorityRegex();

		[GeneratedRegex(@"[^\\/]+")]
		private static partial Regex PathSegmentRegex();

		private static readonly Regex? UserNameSegmentRegex = CreateUserNameSegmentRegex();

		private static readonly char[] SeparatorChars = ['\\', '/'];

		/// <summary>
		/// Returns <paramref name="path"/> with every folder and file name replaced by a
		/// placeholder, keeping only the drive designator, the separators and the file extension.
		/// </summary>
		public static string RedactPath(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return "[Empty]";

			try
			{
				return RedactPathCore(path);
			}
			catch
			{
				return "[?]";
			}
		}

		/// <summary>
		/// Replaces the profile user name and private folder and file names in
		/// <paramref name="message"/> with placeholders so they never reach the log file.
		/// </summary>
		public static string SanitizeMessage(string? message)
		{
			if (string.IsNullOrEmpty(message))
				return string.Empty;

			try
			{
				var lines = message.Split('\n');
				for (int i = 0; i < lines.Length; i++)
				{
					// Scrubbed on every line, stack trace frames included
					lines[i] = EmailRegex().Replace(lines[i], "%Email%");
					lines[i] = UrlAuthorityRegex().Replace(lines[i], "%Host%");

					// Stack trace frames ("   at Method() in <file>:line n") carry build-machine source
					// paths that are needed to read the trace; only the user name is scrubbed there
					if (!lines[i].TrimStart().StartsWith("at ", StringComparison.Ordinal))
						lines[i] = AbsolutePathRegex().Replace(lines[i], ReplacePathToken);
				}

				return RedactUserName(string.Join('\n', lines))!;
			}
			catch (RegexMatchTimeoutException)
			{
				// A message we failed to scrub must not be written as-is
				return "[Unsanitized message dropped]";
			}
		}

		/// <summary>
		/// Replaces user name path segments in <paramref name="text"/> with a placeholder while
		/// keeping the rest of the text intact, for text whose paths must stay readable
		/// (e.g. stack trace source paths).
		/// </summary>
		public static string? RedactUserName(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			text = UserProfileSegmentRegex().Replace(text, "%UserName%");

			if (UserNameSegmentRegex is not null)
				text = UserNameSegmentRegex.Replace(text, "%UserName%");

			return text;
		}

		private static string ReplacePathToken(Match match)
		{
			// A path that already contains the user-name placeholder was redacted at the
			// call site and is meant to stay readable
			if (match.Value.Contains("%UserName%", StringComparison.Ordinal))
				return match.Value;

			// Trailing punctuation belongs to the sentence, not the path
			var path = match.Value.TrimEnd(' ', '.', ',', ';', ')', ']');

			// Bare drive roots ("C:\") carry no private data
			if (path.Length <= 3)
				return match.Value;

			return RedactPathCore(path) + match.Value.Substring(path.Length);
		}

		private static string RedactPathCore(string path)
		{
			var lastSeparator = path.LastIndexOfAny(SeparatorChars);

			return PathSegmentRegex().Replace(path, match =>
			{
				// Drive designator ("C:")
				if (match.Index == 0 && match.Value.Length == 2 && match.Value[1] == ':')
					return match.Value;

				// The extension stays on the last segment for diagnostics
				var extension = match.Index > lastSeparator ? Path.GetExtension(match.Value) : string.Empty;
				return "%Private%" + extension;
			});
		}

		private static Regex? CreateUserNameSegmentRegex()
		{
			// The current user name as a path segment, for paths not rooted under "\Users\"
			// (e.g. redirected profiles or network shares)
			var userName = Environment.UserName;
			if (string.IsNullOrWhiteSpace(userName))
				return null;

			return new Regex($@"(?<=[\\/]){Regex.Escape(userName)}(?=[\\/'""\s]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}
	}
}
