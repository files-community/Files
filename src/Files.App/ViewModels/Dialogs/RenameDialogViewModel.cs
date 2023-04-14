using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.ViewModels.Dialogs
{
    class RenameDialogViewModel : ObservableObject
    {
		private bool isNameInvalid = true;
		public bool IsNameInvalid
		{
			get => isNameInvalid;
			set => SetProperty(ref isNameInvalid, value);
		}
    }
}
