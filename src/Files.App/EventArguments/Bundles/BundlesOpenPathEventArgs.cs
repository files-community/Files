namespace Files.App.EventArguments.Bundles
{
	public class BundlesOpenPathEventArgs
	{
		public readonly string path;

		public readonly FilesystemItemType itemType;

		public readonly bool openSilent;

		public readonly bool openViaApplicationPicker;

		public readonly IEnumerable<string> selectItems;

		public BundlesOpenPathEventArgs(string path, FilesystemItemType itemType, bool openSilent, bool openViaApplicationPicker, IEnumerable<string> selectItems)
		{
			this.path = path;
			this.itemType = itemType;
			this.openSilent = openSilent;
			this.openViaApplicationPicker = openViaApplicationPicker;
			this.selectItems = selectItems;
		}
	}
}
