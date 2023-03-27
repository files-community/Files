using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class SearchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Search".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.F, VirtualKeyModifiers.Control);

		public HotKey SecondHotKey { get; } = new(VirtualKey.F3);

		public RichGlyph Glyph { get; } = new();

		public Task ExecuteAsync()
		{
			context.ShellPage?.ToolbarViewModel.SwitchSearchBoxVisibility();
			return Task.CompletedTask;
		}
	}
}
