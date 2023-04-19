namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	internal class UniversalDateTimeFormatter : AbstractDateTimeFormatter
	{
		public override string Name
			=> "Universal".GetLocalizedResource();

		public override string ToShortLabel(DateTimeOffset offset)
		{
			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			return ToString(offset, "yyyy-MM-dd HH:mm:ss");
		}
	}
}
