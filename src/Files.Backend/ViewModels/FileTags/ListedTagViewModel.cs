using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.Backend.ViewModels.FileTags
{
	public class ListedTagViewModel : ObservableObject
	{
		private TagViewModel tag;
		public TagViewModel Tag 
		{
			get => tag;
			set => SetProperty(ref tag, value);
		}

		private bool isRenaming;
		public bool IsRenaming
		{
			get => isRenaming;
			set => SetProperty(ref isRenaming, value);
		}

		public ListedTagViewModel(TagViewModel tag)
		{
			Tag = tag;
		}
	}
}
