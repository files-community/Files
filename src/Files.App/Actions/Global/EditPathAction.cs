using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class EditPathAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "EditPath".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(Keys.L, KeyModifiers.Ctrl);

		public HotKey SecondHotKey { get; } = new(Keys.D, KeyModifiers.Menu);

		public Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				context.ShellPage.ToolbarViewModel.IsEditModeEnabled = true;

			return Task.CompletedTask;
		}
	}
}
