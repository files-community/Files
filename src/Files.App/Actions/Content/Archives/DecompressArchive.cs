// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Actions
{
	internal class DecompressArchive : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "ExtractFiles".GetLocalizedResource();

		public string Description
			=> "DecompressArchiveDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.E, KeyModifiers.Ctrl);

		public override bool IsExecutable => 
			(IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanDecompress(context.SelectedItems) ||
			CanDecompressInsideArchive()) &&
			UIHelpers.CanShowDialog;

		public DecompressArchive()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ArchiveHelpers.DecompressArchive(context.ShellPage);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
		}

		private bool CanDecompressInsideArchive()
		{
			return
				context.PageType == ContentPageTypes.ZipFolder &&
				!context.HasSelection &&
				context.Folder is not null &&
				FileExtensionHelpers.IsZipFile(Path.GetExtension(context.Folder.ItemPath));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
