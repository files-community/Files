// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CreateShortcutAction : BaseUIAction, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "CreateShortcut".GetLocalizedResource();

		public string Description
			=> "CreateShortcutDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable =>
			ContentPageContext.HasSelection &&
			ContentPageContext.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateShortcutAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return UIFilesystemHelpers.CreateShortcutAsync(ContentPageContext.ShellPage, ContentPageContext.SelectedItems);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.CanCreateItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
