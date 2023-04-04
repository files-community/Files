using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.ViewModels;
using Files.App.Views;
using System.Threading.Tasks;
using static Files.App.ViewModels.MainPageViewModel;

namespace Files.App.Actions
{
	internal class DuplicateSelectedTabAction : IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label { get; } = "DuplicateTab".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(Keys.K, KeyModifiers.CtrlShift);

		public async Task ExecuteAsync()
		{
			var arguments = context.SelectedTabItem.TabItemArguments;
			if (arguments is null)
			{
				await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await mainPageViewModel.AddNewTabByParam(arguments.InitialPageType, arguments.NavigationArg, context.SelectedTabIndex + 1);
			}
		}
	}
}
