// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Helpers;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DecompressArchive : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "ExtractFiles".GetLocalizedResource();

		public string Description => "DecompressArchiveDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.E, KeyModifiers.Ctrl);

		public override bool IsExecutable => 
			(IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanDecompress(context.SelectedItems)
			|| CanDecompressInsideArchive()) &&
			UIHelpers.CanShowDialog;

		public DecompressArchive()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ArchiveHelpers.DecompressArchive(context.ShellPage);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private bool CanDecompressInsideArchive()
		{
			return context.PageType is ContentPageTypes.ZipFolder &&
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
