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
				var page = context.ShellPage;
				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				if (isCommandPaletteOpen && context.HasItem)
					return true;
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasItem)
					return false;

				if (page is null)
					return false;

				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return !isEditing && !isRenaming;
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
