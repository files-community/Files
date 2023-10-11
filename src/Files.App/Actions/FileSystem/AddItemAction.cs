// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class AddItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IDialogService dialogService;

		private readonly AddItemDialogViewModel viewModel = new();

		public string Label
			=> "BaseLayoutContextFlyoutNew/Label".GetLocalizedResource();

		public string Description
			=> "AddItemDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.N, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconNew");

		public bool IsExecutable
			=> context.CanCreateItem;

		public AddItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await UIFilesystemHelpers.CreateFileFromDialogResultType(
			AddItemDialogItemType.Folder, null, context.ShellPage!);

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
