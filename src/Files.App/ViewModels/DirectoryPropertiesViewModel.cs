namespace Files.App.ViewModels
{
	public class DirectoryPropertiesViewModel : ObservableObject
	{
		private string directoryItemCount;
		public string DirectoryItemCount
		{
			get => directoryItemCount;
			set => SetProperty(ref directoryItemCount, value);
		}

		private string? gitBranchDisplayName;
		public string? GitBranchDisplayName
		{
			get => gitBranchDisplayName;
			set => SetProperty(ref gitBranchDisplayName, value);
		}
	}
}
