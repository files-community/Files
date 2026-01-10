// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public abstract partial class TableViewBindableColumn : TableViewColumn
	{
		[GeneratedDependencyProperty]
		public partial Style? ElementStyle { get; set; }

		[GeneratedDependencyProperty]
		public partial Style? EditingElementStyle { get; set; }
	}
}
