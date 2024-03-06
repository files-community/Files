namespace Files.App.Server.Data
{
	public sealed class ColumnPreferencesItem
	{
		public double UserLengthPixels { get; set; }
		public double NormalMaxLength { get; set; } = 800;
		public bool UserCollapsed { get; set; }
	}
}
