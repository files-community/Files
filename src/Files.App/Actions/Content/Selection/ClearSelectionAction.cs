// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ClearSelectionAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.ClearSelection.GetLocalizedResource();

		public string Description
			=> Strings.ClearSelectionDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.SelectNone");

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasSelection)
					return false;

				var page = context.ShellPage;
				if (page is null)
					return false;

				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return !isRenaming;
			}
		}

		public ClearSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.ClearSelection();

			return Task.CompletedTask;
		}
	}
}
