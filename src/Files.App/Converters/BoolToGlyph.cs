namespace Files.App.Converters
{
	public class RecentFilesBoolToGlyph
	{
		public static object Convert(bool value)
		{
			return value ? "\xE81C" : "\xE7C3";
		}
	}
}
