// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class FolderEmptyIndicator : UserControl
	{
		public static readonly DependencyProperty EmptyTextTypeProperty =
			DependencyProperty.Register(
				nameof(EmptyTextType),
				typeof(EmptyTextType),
				typeof(FolderEmptyIndicator),
				new PropertyMetadata(null));

		public EmptyTextType EmptyTextType
		{
			get => (EmptyTextType)GetValue(EmptyTextTypeProperty);
			set => SetValue(EmptyTextTypeProperty, value);
		}

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
