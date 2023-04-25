// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels
{
	public class DirectoryPropertiesViewModel : ObservableObject
	{
		private string directoryItemCount;
		public string DirectoryItemCount
		{
			get => directoryItemCount;
			set => SetProperty(ref directoryItemCount, value);
		}

		private string? gitBranchDisplayName;
		public string? GitBranchDisplayName
		{
			get => gitBranchDisplayName;
			set => SetProperty(ref gitBranchDisplayName, value);
		}
	}
}
