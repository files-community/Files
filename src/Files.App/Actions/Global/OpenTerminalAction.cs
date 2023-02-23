using Files.App.Commands;
using Files.App.Extensions;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenTerminalAction : IAction
	{
		public string Label { get; } = "OpenTerminal".GetLocalizedResource();

		public HotKey HotKey { get; } = new((VirtualKey)192, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new RichGlyph("\uE756");

		public string Path { get; set; } = string.Empty;

		public Task ExecuteAsync()
		{
			var terminalStartInfo = new ProcessStartInfo()
			{
				FileName = "wt.exe",
				Arguments = $"-d {Path}"
			};

			App.Window.DispatcherQueue.TryEnqueue(() => Process.Start(terminalStartInfo));

			return Task.CompletedTask;
		}
	}
}
