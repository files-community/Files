// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CloseOtherTabsCurrentAction : CloseTabBaseAction
	{
		public override string Label
			=> "CloseOtherTabs".GetLocalizedResource();

		public override string Description
			=> "CloseOtherTabsCurrentDescription".GetLocalizedResource();

		public CloseOtherTabsCurrentAction()
		{
		}

		public override Task ExecuteAsync()
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseOtherTabs(context.CurrentTabItem, context.Control);

			return Task.CompletedTask;
		}
	}
}
