// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Files.App.UserControls.FilePreviews
{
	internal static class CssPreviewRenderer
	{
		public static void Render(RichTextBlock target, string text, bool isDark)
		{
			var paragraph = new Paragraph();
			var inComment = false;

			var defaultBrush = new SolidColorBrush(isDark ? Colors.White : Colors.Black);
			var commentBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 106, 153, 85) : Windows.UI.Color.FromArgb(255, 0, 128, 0));
			var keywordBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 197, 134, 192) : Windows.UI.Color.FromArgb(255, 175, 0, 219));
			var propertyBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 156, 220, 254) : Windows.UI.Color.FromArgb(255, 4, 81, 165));
			var stringBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 206, 145, 120) : Windows.UI.Color.FromArgb(255, 163, 21, 21));

			var lines = text.Split('\n');
			for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];
				var i = 0;

				while (i < line.Length)
				{
					if (inComment)
					{
						var commentEnd = line.IndexOf("*/", i, StringComparison.Ordinal);
						if (commentEnd < 0)
						{
							AppendRun(paragraph, line[i..], commentBrush);
							i = line.Length;
							break;
						}

						AppendRun(paragraph, line[i..(commentEnd + 2)], commentBrush);
						i = commentEnd + 2;
						inComment = false;
						continue;
					}

					if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
					{
						var commentEnd = line.IndexOf("*/", i + 2, StringComparison.Ordinal);
						if (commentEnd < 0)
						{
							AppendRun(paragraph, line[i..], commentBrush);
							inComment = true;
							i = line.Length;
							break;
						}

						AppendRun(paragraph, line[i..(commentEnd + 2)], commentBrush);
						i = commentEnd + 2;
						continue;
					}

					if (line[i] is '\'' or '"')
					{
						var quote = line[i];
						var start = i;
						i++;

						while (i < line.Length)
						{
							if (line[i] == '\\')
							{
								i += 2;
								continue;
							}

							if (line[i] == quote)
							{
								i++;
								break;
							}

							i++;
						}

						AppendRun(paragraph, line[start..Math.Min(i, line.Length)], stringBrush);
						continue;
					}

					if (line[i] == '@')
					{
						var start = i;
						i++;
						while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] is '-' or '_'))
							i++;
						AppendRun(paragraph, line[start..i], keywordBrush);
						continue;
					}

					if (char.IsLetter(line[i]) || line[i] is '-' or '_')
					{
						var start = i;
						i++;
						while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] is '-' or '_'))
							i++;

						var lookahead = i;
						while (lookahead < line.Length && char.IsWhiteSpace(line[lookahead]))
							lookahead++;

						var token = line[start..i];
						AppendRun(paragraph, token, lookahead < line.Length && line[lookahead] == ':' ? propertyBrush : defaultBrush);
						continue;
					}

					AppendRun(paragraph, line[i].ToString(), defaultBrush);
					i++;
				}

				if (lineIndex < lines.Length - 1)
					AppendRun(paragraph, "\n", defaultBrush);
			}

			target.Blocks.Add(paragraph);
		}

		private static void AppendRun(Paragraph paragraph, string text, SolidColorBrush brush)
		{
			if (text.Length == 0)
				return;

			paragraph.Inlines.Add(new Run { Text = text, Foreground = brush });
		}
	}
}
