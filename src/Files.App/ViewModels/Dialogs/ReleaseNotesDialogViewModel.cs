// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
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
