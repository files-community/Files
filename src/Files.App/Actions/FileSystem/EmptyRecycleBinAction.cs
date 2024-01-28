// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EmptyRecycleBinAction : BaseUIAction, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<ContentPageContext>();
		private ITrashService RecycleBinService { get; } = Ioc.Default.GetRequiredService<ITrashService>();

		public string Label
			=> "EmptyRecycleBin".GetLocalizedResource();

		public string Description
			=> "EmptyRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconDelete");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			((ContentPageContext.PageType == ContentPageTypes.RecycleBin && ContentPageContext.HasItem) ||
			RecycleBinService.HasItems());

		public EmptyRecycleBinAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await RecycleBinService.EmptyTrashAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
