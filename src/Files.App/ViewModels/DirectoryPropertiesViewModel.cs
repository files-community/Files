using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.ViewModels
{
	public class DirectoryPropertiesViewModel : ObservableObject
	{
		private string _directoryItemCount;
		public string DirectoryItemCount
		{
			get => _directoryItemCount;
			set => SetProperty(ref _directoryItemCount, value);
		}
	}
}
