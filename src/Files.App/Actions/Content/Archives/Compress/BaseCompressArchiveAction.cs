// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal abstract class BaseCompressArchiveAction : BaseUIAction, IAction
	{
		protected readonly IContentPageContext context;

		public abstract string Label { get; }

		public abstract string Description { get; }

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			CompressHelper.CanCompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public BaseCompressArchiveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync();

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
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
