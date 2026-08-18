// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
{
	[global::WinRT.GeneratedBindableCustomProperty]
	partial class CreateItemDialogViewModel : ObservableObject
	{
		private bool isNameInvalid;
		public bool IsNameInvalid
		{
			get => isNameInvalid;
			set => SetProperty(ref isNameInvalid, value);
		}
	}
}
