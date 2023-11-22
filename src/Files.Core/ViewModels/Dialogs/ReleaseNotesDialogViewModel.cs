// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.ViewModels.Dialogs
{
	public sealed class ReleaseNotesDialogViewModel : ObservableObject
	{
		private string _ReleaseNotesMadrkdown = string.Empty;
		public string ReleaseNotesMadrkdown
		{
			get => _ReleaseNotesMadrkdown;
			set => SetProperty(ref _ReleaseNotesMadrkdown, value);
		}

		public ReleaseNotesDialogViewModel(string releaseNotesMadrkdown)
		{
			ReleaseNotesMadrkdown = releaseNotesMadrkdown;
		}
	}
}
