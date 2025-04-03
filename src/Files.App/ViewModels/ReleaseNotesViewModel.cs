// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels
{
	public sealed partial class ReleaseNotesViewModel : ObservableObject
	{
		public string BlogPostUrl =>
			Constants.ExternalUrl.ReleaseNotesUrl;

		public ReleaseNotesViewModel()
		{
		}
	}
}
