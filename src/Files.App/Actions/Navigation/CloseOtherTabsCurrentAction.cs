// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class CloseOtherTabsCurrentAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseOtherTabs.GetLocalizedResource();

		public override string Description
			=> Strings.CloseOtherTabsCurrentDescription.GetLocalizedResource();

		public CloseOtherTabsCurrentAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseOtherTabs(context.CurrentTabItem, context.Control);

			return Task.CompletedTask;
		}
	}
}
