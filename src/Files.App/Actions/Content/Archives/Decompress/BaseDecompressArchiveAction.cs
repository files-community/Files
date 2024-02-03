// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal abstract class BaseDecompressArchiveAction : BaseUIAction, IAction
	{
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public virtual HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			(IsContextPageTypeAdaptedToCommand() &&
			CompressHelper.CanDecompress(ContentPageContext.SelectedItems) ||
			CanDecompressInsideArchive()) &&
			UIHelpers.CanShowDialog;

		public BaseDecompressArchiveAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync();

		protected bool IsContextPageTypeAdaptedToCommand()
		{
			return
				ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
				ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
				ContentPageContext.PageType != ContentPageTypes.None;
		}

		protected virtual bool CanDecompressInsideArchive()
		{
			return false;
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
