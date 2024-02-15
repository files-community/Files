// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class InvertSelectionAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "InvertSelection".GetLocalizedResource();

		public string Description
			=> "InvertSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE746");

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasItem)
					return false;

				var page = context.ShellPage;
				if (page is null)
					return false;

				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return isCommandPaletteOpen || (!isEditing && !isRenaming);
			}
		}

		public InvertSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.InvertSelection();

			return Task.CompletedTask;
		}
	}
}
