// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
{
    class CreateItemDialogViewModel : ObservableObject
    {
		private bool isNameInvalid;
		public bool IsNameInvalid
		{
			get => isNameInvalid;
			set => SetProperty(ref isNameInvalid, value);
		}
    }
}
