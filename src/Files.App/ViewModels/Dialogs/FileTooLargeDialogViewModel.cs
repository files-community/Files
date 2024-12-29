// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
{
	public sealed class FileTooLargeDialogViewModel: ObservableObject
	{
		public IEnumerable<string> Paths { get; private set; }

		public FileTooLargeDialogViewModel(IEnumerable<string> paths) 
		{ 
			Paths = paths;
		}
	}
}
