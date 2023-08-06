// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.ViewModels.Dialogs
{
	public sealed class FileTooLargeDialogViewModel: ObservableObject
	{
		public string ListHeader { get; private set; }

		public IEnumerable<string> Paths { get; private set; }

		public FileTooLargeDialogViewModel(string listHeader, IEnumerable<string> paths) 
		{ 
			ListHeader = listHeader;
			Paths = paths;
		}
	}
}
