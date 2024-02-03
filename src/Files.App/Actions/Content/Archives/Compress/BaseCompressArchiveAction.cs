// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal abstract class BaseCompressArchiveAction : BaseUIAction, IAction
	{
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			CompressHelper.CanCompress(ContentPageContext.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public BaseCompressArchiveAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync();

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
				ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
				ContentPageContext.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
