// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class PasteItemToSelectionAction : BaseUIAction, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Paste".GetLocalizedResource();

		public string Description
			=> "PasteItemToSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPaste");

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.CtrlShift);

		public override bool IsExecutable
			=> GetIsExecutable();

		public PasteItemToSelectionAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is null)
				return;

			string path = ContentPageContext.SelectedItem is ListedItem selectedItem
				? selectedItem.ItemPath
				: ContentPageContext.ShellPage.FilesystemViewModel.WorkingDirectory;

			await UIFilesystemHelpers.PasteItemAsync(path, ContentPageContext.ShellPage);
		}

		public bool GetIsExecutable()
		{
			if (!App.AppModel.IsPasteEnabled)
				return false;

			if (ContentPageContext.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults)
				return false;

			if (!ContentPageContext.HasSelection)
				return true;

			return
				ContentPageContext.SelectedItem?.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder &&
				UIHelpers.CanShowDialog;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
