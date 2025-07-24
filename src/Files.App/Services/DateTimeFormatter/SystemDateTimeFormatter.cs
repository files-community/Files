// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.DateTimeFormatter
{
	internal sealed class SystemDateTimeFormatter : AbstractDateTimeFormatter
	{
		public override string Name
			=> Strings.SystemTimeStyle.GetLocalizedResource();

		public override string ToShortLabel(DateTimeOffset offset)
		{
			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			return ToString(offset, "g");
		}
	}
}
