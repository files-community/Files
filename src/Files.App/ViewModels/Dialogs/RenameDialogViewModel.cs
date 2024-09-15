// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

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
