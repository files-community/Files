// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class TableViewCell
	{
		[GeneratedDependencyProperty]
		public partial bool HasValidationError { get; private set; }

		[GeneratedDependencyProperty]
		public partial object? ValidationError { get; private set; }

		partial void OnValidationErrorChanged(object? newValue)
		{
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(this, newValue?.ToString() ?? string.Empty);
		}

		partial void OnHasValidationErrorChanged(bool newValue)
		{
			UpdateValidationVisualState(true);
		}
	}
}
