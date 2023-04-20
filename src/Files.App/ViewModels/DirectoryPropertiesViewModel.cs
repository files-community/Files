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
	}
}
