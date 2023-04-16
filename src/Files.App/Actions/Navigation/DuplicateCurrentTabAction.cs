using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Views;
using System.Threading.Tasks;
using static Files.App.ViewModels.MainPageViewModel;

namespace Files.App.Actions
{
	internal class DuplicateCurrentTabAction : IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "DuplicateTab".GetLocalizedResource();
		public string Description => "DuplicateCurrentTabDescription".GetLocalizedResource();

		public async Task ExecuteAsync()
		{
			var arguments = context.CurrentTabItem.TabItemArguments;
			if (arguments is null)
			{
				await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await AddNewTabByParam(arguments.InitialPageType, arguments.NavigationArg, context.CurrentTabIndex + 1);
			}
		}
	}
}
