namespace Files.App.Data.EventArguments
{
	public class WorkingDirectoryModifiedEventArgs : EventArgs
	{
		public string? Path { get; set; }

		public string? Name { get; set; }

		public bool IsLibrary { get; set; }
	}
}
