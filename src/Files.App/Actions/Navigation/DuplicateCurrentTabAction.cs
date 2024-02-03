// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DuplicateCurrentTabAction : IAction
	{
		private IMultitaskingContext MultitaskingContext { get; } = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateCurrentTabDescription".GetLocalizedResource();

		public DuplicateCurrentTabAction()
		{
		}

		public async Task ExecuteAsync()
		{
			var arguments = MultitaskingContext.CurrentTabItem.NavigationParameter;

			if (arguments is null)
			{
				await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await NavigationHelpers.AddNewTabByParamAsync(
					arguments.InitialPageType,
					arguments.NavigationParameter,
					MultitaskingContext.CurrentTabIndex + 1);
			}
		}
	}
}
