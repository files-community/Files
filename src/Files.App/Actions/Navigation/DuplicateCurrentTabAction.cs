using Files.App.Contexts;
using Files.App.ViewModels;
using Files.App.Views;

namespace Files.App.Actions
{
	internal class DuplicateCurrentTabAction : IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label { get; } = "DuplicateTab".GetLocalizedResource();
		public string Description => "DuplicateCurrentTabDescription".GetLocalizedResource();

		public async Task ExecuteAsync()
		{
			var arguments = context.CurrentTabItem.TabItemArguments;
			if (arguments is null)
			{
				await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await mainPageViewModel.AddNewTabByParam(arguments.InitialPageType, arguments.NavigationArg, context.CurrentTabIndex + 1);
			}
		}
	}
}
