// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared.Services.DateTimeFormatter
{
	public interface ITimeSpanLabel
	{
		string Text { get; }
		string Glyph { get; }
		int Index { get; }
	}
}
