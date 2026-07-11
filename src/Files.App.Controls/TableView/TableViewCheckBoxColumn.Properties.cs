// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Controls
{
	public partial class TableViewCheckBoxColumn
	{
		[GeneratedDependencyProperty]
		public partial string? IsEnabledBinding { get; set; }

		[GeneratedDependencyProperty]
		public partial string? VisibilityBinding { get; set; }

		[GeneratedDependencyProperty]
		public partial IValueConverter? VisibilityConverter { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsThreeState { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsReadOnly { get; set; }
	}
}
