using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class NewTabAction : IAction
	{
		public string Label { get; } = "NewTab".GetLocalizedResource();

		public HotKey HotKey { get; } = new(VirtualKey.T, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new("\uE710");

		public Task ExecuteAsync() => MainPageViewModel.AddNewTabAsync();
	}
}
