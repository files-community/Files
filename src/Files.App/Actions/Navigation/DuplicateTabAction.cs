using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class DuplicateTabAction : IAction
	{
		public string Label { get; } = "DuplicateTab".GetLocalizedResource();

		public HotKey HotKey { get; } = new(VirtualKey.K, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public Task ExecuteAsync() => MainPageViewModel.DuplicateTabAsync();
	}
}
