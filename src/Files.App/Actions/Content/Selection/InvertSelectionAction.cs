// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class InvertSelectionAction : ObservableObject, IAction
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

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.InvertSelection();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasItem):
				case nameof(IContentPageContext.ShellPage):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
