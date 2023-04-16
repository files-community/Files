using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class NewTabAction : IAction
	{
		public string Label { get; } = "NewTab".GetLocalizedResource();

		public string Description => "NewTabDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.T, KeyModifiers.Ctrl);

		public Task ExecuteAsync() => MainPageViewModel.AddNewTabAsync();
	}
}
