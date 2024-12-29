// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	[DependencyProperty<EmptyTextType>("EmptyTextType")]
	public sealed partial class FolderEmptyIndicator : UserControl
	{
		public FolderEmptyIndicator()
		{
			InitializeComponent();
		}
	}

	public enum EmptyTextType
	{
		None,
		FolderEmpty,
		NoSearchResultsFound,
	}
}
