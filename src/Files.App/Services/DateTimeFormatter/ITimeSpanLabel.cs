// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Services.DateTimeFormatter
{
	public interface ITimeSpanLabel
	{
		string Text { get; }

		string Glyph { get; }

		int Index { get; }
	}
}
