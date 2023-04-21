namespace Files.App.ViewModels.Dialogs
{
    class RenameDialogViewModel : ObservableObject
    {
		private bool isNameInvalid;
		public bool IsNameInvalid
		{
			get => isNameInvalid;
			set => SetProperty(ref isNameInvalid, value);
		}
    }
}
