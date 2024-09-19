// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Files.Shared.Helpers;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class BulkRenameDialogViewModel : ObservableObject
	{
		// Properties

		public bool IsNameValid =>
			FilesystemHelpers.IsValidForFilename(fileName);
		
		public bool ShowNameWarning =>
			!string.IsNullOrEmpty(fileName) && !IsNameValid;

		
		private string fileName = string.Empty;
		public string FileName
		{
			get => fileName;
			set
			{
				if (SetProperty(ref fileName, value))
				{
					OnPropertyChanged(nameof(IsNameValid));
					OnPropertyChanged(nameof(ShowNameWarning));
				}
			}
		}

		public BulkRenameDialogViewModel()
		{
			
		}
	}
}