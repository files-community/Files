// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
