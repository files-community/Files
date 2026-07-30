// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CloseOtherTabsSelectedAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseOtherTabs.GetLocalizedResource();

		public override string Description
			=> Strings.CloseOtherTabsSelectedDescription.GetLocalizedResource();

		public CloseOtherTabsSelectedAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedTabItem is { } selectedTabItem && context.Control is { } control)
				MultitaskingTabsHelpers.CloseOtherTabs(selectedTabItem, control);

			return Task.CompletedTask;
		}
	}
}
