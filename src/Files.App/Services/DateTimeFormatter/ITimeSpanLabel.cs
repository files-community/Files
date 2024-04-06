﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.DateTimeFormatter
{
	public interface ITimeSpanLabel
	{
		string Text { get; }

		string Glyph { get; }

		int Index { get; }
	}
}
