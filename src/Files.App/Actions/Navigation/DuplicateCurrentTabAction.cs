// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class DuplicateCurrentTabAction : IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> Strings.DuplicateTab.GetLocalizedResource();

		public string Description
			=> Strings.DuplicateCurrentTabDescription.GetLocalizedResource();

		public DuplicateCurrentTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var arguments = context.CurrentTabItem.NavigationParameter;

			if (arguments is null)
			{
				await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), "Home", true);
			}
			else
			{
				await NavigationHelpers.AddNewTabByParamAsync(
					arguments.InitialPageType,
					arguments.NavigationParameter,
					context.CurrentTabIndex + 1);
			}
		}
	}
}
