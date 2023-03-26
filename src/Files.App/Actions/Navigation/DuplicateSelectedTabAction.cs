using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Views;
using System.Threading.Tasks;
using Windows.System;
using static Files.App.ViewModels.MainPageViewModel;

namespace Files.App.Actions
{
	internal class DuplicateSelectedTabAction : IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "DuplicateTab".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.K, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public async Task ExecuteAsync()
		{
			var arguments = context.SelectedTabItem.TabItemArguments;
			if (arguments is null)
			{
				await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await AddNewTabByParam(arguments.InitialPageType, arguments.NavigationArg, context.SelectedTabIndex + 1);
			}
		}
	}
}
