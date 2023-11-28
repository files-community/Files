// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class CopyPathWithQuotesAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CopyPathWithQuotes".GetLocalizedResource();

		public string Description
			=> "CopyPathWithQuotesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(opacityStyle: "ColorIconCopyPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.MenuCtrl);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyPathWithQuotesAction()
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
