// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DuplicateCurrentTabAction : IAction
	{
		private readonly IMultitaskingContext context;

		private readonly MainPageViewModel mainPageViewModel;

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateCurrentTabDescription".GetLocalizedResource();

		public DuplicateCurrentTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		}

		public async Task ExecuteAsync()
		{
			var arguments = context.CurrentTabItem.NavigationParameter;
			if (arguments is null)
				await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			else
				await mainPageViewModel.AddNewTabByParamAsync(arguments.InitialPageType, arguments.NavigationParameter, context.CurrentTabIndex + 1);
		}
	}
}
