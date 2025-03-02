// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class ReleaseNotesDialogViewModel : ObservableObject
	{
		private string _BlogPostUrl = string.Empty;
		public string BlogPostUrl
		{
			get => _BlogPostUrl;
			set => SetProperty(ref _BlogPostUrl, value);
		}

		public ReleaseNotesDialogViewModel(string blogPostUrl)
		{
			BlogPostUrl = blogPostUrl;
		}
	}
}
