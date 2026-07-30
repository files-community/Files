// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class CopyItemPathWithQuotesAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyItemPathWithQuotes.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemPathWithQuotesDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlAlt);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyItemPathWithQuotesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0)
			{
				var path = string.Join("\n", context.SelectedItems.Select(item => $"\"{item.ItemPath}\""));

				if (FtpHelpers.IsFtpPath(path))
					path = path.Replace('\\', '/');

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
