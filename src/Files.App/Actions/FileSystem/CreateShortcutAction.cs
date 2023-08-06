// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CreateShortcutAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateShortcut".GetLocalizedResource();

		public string Description
			=> "CreateShortcutDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable =>
			context.HasSelection &&
			context.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateShortcutAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return UIFilesystemHelpers.CreateShortcutAsync(context.ShellPage, context.SelectedItems);
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
