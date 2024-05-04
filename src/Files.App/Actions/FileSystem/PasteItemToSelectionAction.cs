// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class PasteItemToSelectionAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;
		private IWindowContext WindowContext { get; } = Ioc.Default.GetRequiredService<IWindowContext>();

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
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			WindowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			string path = context.SelectedItem is ListedItem selectedItem
				? selectedItem.ItemPath
				: context.ShellPage.FilesystemViewModel.WorkingDirectory;

			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			if (!App.WindowContext.IsPasteEnabled)
				return false;

			if (context.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults)
				return false;

			if (!context.HasSelection)
				return true;

			return
				context.SelectedItem?.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder &&
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
		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IWindowContext.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
