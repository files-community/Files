// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class AddItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IDialogService DialogService { get; } = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly AddItemDialogViewModel viewModel = new();

		public string Label
			=> "BaseLayoutContextFlyoutNew/Label".GetLocalizedResource();

		public string Description
			=> "AddItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconNew");

		public HotKey HotKey
			=> new(Keys.I, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> ContentPageContext.CanCreateItem;

		public AddItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await DialogService.ShowDialogAsync(viewModel);

			if (viewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
			{
				await Ioc.Default.GetRequiredService<ICommandManager>().CreateShortcutFromDialog.ExecuteAsync();
			}
			else if (viewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
			{
				await UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(
					viewModel.ResultType.ItemType,
					viewModel.ResultType.ItemInfo,
					ContentPageContext.ShellPage!);
			}

			viewModel.ResultType.ItemType = AddItemDialogItemType.Cancel;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanCreateItem):
				case nameof(IContentPageContext.HasSelection):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
