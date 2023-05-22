// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;
using System;

namespace Files.Shared.Services.DateTimeFormatter
{
	public interface IDateTimeFormatter
	{
		string Name { get; }

		string ToShortLabel(DateTimeOffset offset);
		string ToLongLabel(DateTimeOffset offset);

		ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit);
	}
}
