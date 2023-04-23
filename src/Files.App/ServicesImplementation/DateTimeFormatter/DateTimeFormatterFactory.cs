// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;
using System;

namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	public class DateTimeFormatterFactory : IDateTimeFormatterFactory
	{
		public IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat) => dateTimeFormat switch
		{
			DateTimeFormats.Application => new ApplicationDateTimeFormatter(),
			DateTimeFormats.System => new SystemDateTimeFormatter(),
			DateTimeFormats.Universal => new UniversalDateTimeFormatter(),
			_ => throw new ArgumentException(nameof(dateTimeFormat)),
		};
	}
}
