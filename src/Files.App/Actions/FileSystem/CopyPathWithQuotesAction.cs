// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class CopyPathWithQuotesAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "CopyPathWithQuotes".GetLocalizedResource();

		public string Description
			=> "CopyPathWithQuotesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(opacityStyle: "ColorIconCopyPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.MenuCtrl);

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public CopyPathWithQuotesAction()
		{
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage?.SlimContentPage is not null)
			{
				var selectedItems = ContentPageContext.ShellPage.SlimContentPage.SelectedItems;
				var path = selectedItems is not null
					? string.Join("\n", selectedItems.Select(item => $"\"{item.ItemPath}\""))
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
