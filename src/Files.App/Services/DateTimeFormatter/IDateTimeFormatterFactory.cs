// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Services.DateTimeFormatter
{
	public interface IDateTimeFormatterFactory
	{
		IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
	}
}
