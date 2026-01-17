// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

#pragma warning disable WCTDPG0009 // Non-nullable dependency property is not guaranteed to not be null

namespace Files.App.Controls
{
	public partial class DragSelectionContainer
	{
		[GeneratedDependencyProperty]
		public partial IList<DragSelectionTarget> Targets { get; set; }
	}
}
