// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class FolderEmptyIndicator : UserControl
	{
		[GeneratedDependencyProperty]
		public partial EmptyTextType EmptyTextType { get; set; }

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
