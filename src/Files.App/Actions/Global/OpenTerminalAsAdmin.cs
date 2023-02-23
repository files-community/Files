using Files.App.Commands;
using Files.App.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenTerminalAsAdminAction : IAction
	{
		public string Label { get; } = "OpenTerminalAsAdmin".GetLocalizedResource();

		public HotKey HotKey { get; } = new((VirtualKey)192, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public RichGlyph Glyph { get; } = new RichGlyph("\uE756");

		public string Path { get; set; } = string.Empty;

		public Task ExecuteAsync()
		{
			var terminalStartInfo = new ProcessStartInfo()
			{
				FileName = "wt.exe",
				Arguments = $"-d {Path}",
				Verb = "runas",
				UseShellExecute = true
			};

			try
			{
				App.Window.DispatcherQueue.TryEnqueue(() => Process.Start(terminalStartInfo));
			}
			catch (OperationCanceledException)
			{ 
			}

			return Task.CompletedTask;
		}
	}
}