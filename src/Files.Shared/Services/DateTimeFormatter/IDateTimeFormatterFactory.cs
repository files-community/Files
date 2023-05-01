// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;

namespace Files.Shared.Services.DateTimeFormatter
{
	public interface IDateTimeFormatterFactory
	{
		IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
	}
}
