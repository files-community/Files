// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using System.Text;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class MarkdownPreviewViewModel : BasePreviewModel
	{
		private string textValue;
		public string TextValue
		{
			get => textValue;
			private set => SetProperty(ref textValue, value);
		}

		public MarkdownPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var text = await ReadFileAsTextAsync(Item.ItemFile);
			TextValue = EscapeRawHtml(text.Left(Constants.PreviewPane.TextCharacterLimit));

			return [];
		}

		// Escape raw '<' outside code so Markdig doesn't emit HtmlInline nodes — those crash MarkdownTextBlock's HtmlWriter.WriteHtml.
		private static string EscapeRawHtml(string markdown)
		{
			if (string.IsNullOrEmpty(markdown))
				return markdown;

			var sb = new StringBuilder(markdown.Length);
			var inFencedBlock = false;

			var start = 0;
			for (var i = 0; i <= markdown.Length; i++)
			{
				if (i == markdown.Length || markdown[i] == '\n')
				{
					var lineLen = i - start;
					var line = markdown.AsSpan(start, lineLen);
					var trimmed = line.TrimStart();

					if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
					{
						inFencedBlock = !inFencedBlock;
						sb.Append(line);
					}
					else if (inFencedBlock)
					{
						sb.Append(line);
					}
					else
					{
						AppendLineWithEscapes(sb, line);
					}

					if (i < markdown.Length)
						sb.Append('\n');

					start = i + 1;
				}
			}

			return sb.ToString();
		}

		private static void AppendLineWithEscapes(StringBuilder sb, ReadOnlySpan<char> line)
		{
			var inInlineCode = false;
			for (var j = 0; j < line.Length; j++)
			{
				var ch = line[j];
				if (ch == '`')
				{
					inInlineCode = !inInlineCode;
					sb.Append(ch);
				}
				else if (ch == '<' && !inInlineCode)
				{
					sb.Append("&lt;");
				}
				else
				{
					sb.Append(ch);
				}
			}
		}
	}
}
