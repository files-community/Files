// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;

namespace Files.App.Actions
{
	internal class AddItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

        private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

        private readonly AddItemDialogViewModel viewModel = new();

		public string Label { get; } = "BaseLayoutContextFlyoutNew/Label".GetLocalizedResource();

		public string Description => "AddItemDescription".GetLocalizedResource();

        public HotKey HotKey { get; } = new(Keys.N, KeyModifiers.CtrlShift);

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconNew");

		public bool IsExecutable => context.CanCreateItem && !context.HasSelection;

		public AddItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await dialogService.ShowDialogAsync(viewModel);

			if (viewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
				await Ioc.Default.GetRequiredService<ICommandManager>().CreateShortcutFromDialog.ExecuteAsync();
			else if (viewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
				UIFilesystemHelpers.CreateFileFromDialogResultType(
				viewModel.ResultType.ItemType,
				viewModel.ResultType.ItemInfo,
				context.ShellPage!);
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
