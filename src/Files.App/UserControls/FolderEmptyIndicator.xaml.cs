// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class FolderEmptyIndicator : UserControl
	{
		public EmptyTextType EmptyTextType
		{
			get { return (EmptyTextType)GetValue(EmptyTextTypeProperty); }
			set { SetValue(EmptyTextTypeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for EmptyTextType.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EmptyTextTypeProperty =
			DependencyProperty.Register("EmptyTextType", typeof(EmptyTextType), typeof(FolderEmptyIndicator), new PropertyMetadata(null));

		private string GetTranslated(string resourceName) => resourceName.GetLocalizedResource();

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