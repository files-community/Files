// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.VisualBasic.Devices;
using Windows.ApplicationModel.DataTransfer;
using static Vanara.PInvoke.User32.RAWINPUT;

namespace Files.App.Actions
{
	internal class CopyPathAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CopyPath".GetLocalizedResource();

		public string Description
			=> "CopyPathDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(opacityStyle: "ColorIconCopyPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyPathAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			if (context.ShellPage?.SlimContentPage is not null)
			{
				var path = context.ShellPage.SlimContentPage.SelectedItems is not null
					? context.ShellPage.SlimContentPage.SelectedItems.Select(x => x.ItemPath).Aggregate((accum, current) => accum + "\n" + current)
					: context.ShellPage.FilesystemViewModel.WorkingDirectory;

				if (FtpHelpers.IsFtpPath(path))
					path = path.Replace("\\", "/", StringComparison.Ordinal);

				// Check if Ctrl + Shift is pressed, if so put quotes arround the path
				var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				if (ctrlPressed && shiftPressed)
					path = "\"" + path + "\"";

				SafetyExtensions.IgnoreExceptions(() =>
				{
					DataPackage data = new();
					data.SetText(path);

					Clipboard.SetContent(data);
					Clipboard.Flush();
				});
			}

			return Task.CompletedTask;
		}
	}
}
