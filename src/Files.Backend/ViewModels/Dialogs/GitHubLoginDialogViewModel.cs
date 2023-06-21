// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Files.Backend.ViewModels.Dialogs
{
	public sealed class GitHubLoginDialogViewModel : ObservableObject
	{
		private const string URL = "https://github.com/login/device";

		public string UserCode { get; init; }

		public Uri NavigateUri { get; init; }

		public string LoginUrl { get; init; }
		
		public GitHubLoginDialogViewModel(string userCode)
		{
			UserCode = userCode;
			LoginUrl = URL;
			NavigateUri = new Uri(URL);
		}
	}
}
