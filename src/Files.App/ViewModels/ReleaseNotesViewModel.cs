// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
