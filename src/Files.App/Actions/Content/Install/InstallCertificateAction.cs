// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Shell;
using Files.Backend.Helpers;

namespace Files.App.Actions
{
	internal class InstallCertificateAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Install".GetLocalizedResource();

		public string Description => "InstallCertificateDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uEB95");

		public bool IsExecutable => context.SelectedItems.Any() &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsCertificateFile(x.FileExtension)) &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder;

		public InstallCertificateAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("add", context.SelectedItems.Select(x => x.ItemPath).ToArray());
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.SelectedItems))
			{
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
