// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
