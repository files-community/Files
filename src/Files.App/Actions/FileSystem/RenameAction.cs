// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RenameAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Rename".GetLocalizedResource();

		public string Description
			=> "RenameDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F2);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRename");

		public bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			IsPageTypeValid() &&
			ContentPageContext.ShellPage.SlimContentPage is not null &&
			IsSelectionValid();

		public RenameAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			ContentPageContext.ShellPage?.SlimContentPage?.ItemManipulationModel.StartRenameItem();

			return Task.CompletedTask;
		}

		private bool IsSelectionValid()
		{
			return ContentPageContext.HasSelection && ContentPageContext.SelectedItems.Count == 1;
		}

		private bool IsPageTypeValid()
		{
			return
				ContentPageContext.PageType != ContentPageTypes.None &&
				ContentPageContext.PageType != ContentPageTypes.Home &&
				ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
				ContentPageContext.PageType != ContentPageTypes.ZipFolder;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
