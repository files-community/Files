// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	[Flags]
	internal enum TableViewNotificationTarget
	{
		None = 0,
		ColumnHeaders = 1,
		VisibleRows = 2,
		RowLayout = 4,
		ColumnLayout = 8,
		ResizeVisuals = 16,
	}
}
