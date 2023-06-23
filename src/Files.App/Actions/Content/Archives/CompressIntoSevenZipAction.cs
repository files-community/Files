// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Archive;

namespace Files.App.Actions
{
	internal class CompressIntoSevenZipAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{ArchiveHelpers.DetermineArchiveNameFromSelection(context.SelectedItems)}.7z");

		public string Description
			=> "CompressIntoSevenZipDescription".GetLocalizedResource();

		public bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanCompress(context.SelectedItems);

		public CompressIntoSevenZipAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var (sources, directory, fileName) = ArchiveHelpers.GetCompressDestination(context.ShellPage);

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = fileName,
				FileFormat = ArchiveFormats.SevenZip,
			};

			return ArchiveHelpers.CompressArchiveAsync(creator);
		}

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
