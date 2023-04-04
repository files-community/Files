using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class NewTabAction : IAction
	{
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label { get; } = "NewTab".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.T, VirtualKeyModifiers.Control);

		public Task ExecuteAsync() => mainPageViewModel.AddNewTabAsync();
	}
}
