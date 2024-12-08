// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
