// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class TableViewCell
	{
		[GeneratedDependencyProperty]
		public partial bool HasValidationError { get; private set; }

		partial void OnHasValidationErrorChanged(bool newValue)
		{
			UpdateValidationVisualState(true);
		}
	}
}
