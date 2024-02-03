// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class CopyPathAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "CopyPath".GetLocalizedResource();

		public string Description
			=> "CopyPathDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconCopyPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public CopyPathAction()
		{
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage?.SlimContentPage is not null)
			{
				var path = ContentPageContext.ShellPage.SlimContentPage.SelectedItems is not null
					? ContentPageContext.ShellPage.SlimContentPage.SelectedItems.Select(x => x.ItemPath).Aggregate((accum, current) => accum + "\n" + current)
					: ContentPageContext.ShellPage.FilesystemViewModel.WorkingDirectory;

				if (FtpHelpers.IsFtpPath(path))
					path = path.Replace("\\", "/", StringComparison.Ordinal);

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
