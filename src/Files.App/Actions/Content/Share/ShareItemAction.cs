// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class ShareItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Share".GetLocalizedResource();

		public string Description
			=> "ShareItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconShare");

		public bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			DataTransferManager.IsSupported() &&
			context.SelectedItems.Any() &&
			context.SelectedItems.All(ShareItemHelpers.IsItemShareable);

		public ShareItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			ShareItemHelpers.ShareItems(context.SelectedItems);

			return Task.CompletedTask;
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.Ftp &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
