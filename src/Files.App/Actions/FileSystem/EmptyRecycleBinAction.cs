// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EmptyRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "EmptyRecycleBin".GetLocalizedResource();

		public string Description
			=> "EmptyRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconDelete");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			((context.PageType == ContentPageTypes.RecycleBin && context.HasItem) ||
			RecycleBinHelpers.RecycleBinHasItems());

		public EmptyRecycleBinAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.EmptyRecycleBinAsync();
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
