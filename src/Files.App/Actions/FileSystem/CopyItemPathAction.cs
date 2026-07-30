// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class CopyItemPathAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyItemPath.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemPathDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyItemPathAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0)
			{
				var path = string.Join('\n', context.SelectedItems.Select(x => x.ItemPath));

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
