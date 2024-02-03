// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class InstallCertificateAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Install".GetLocalizedResource();

		public string Description
			=> "InstallCertificateDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uEB95");

		public bool IsExecutable =>
			ContentPageContext.SelectedItems.Any() &&
			ContentPageContext.SelectedItems.All(x => FileExtensionHelpers.IsCertificateFile(x.FileExtension)) &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder;

		public InstallCertificateAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("add", ContentPageContext.SelectedItems.Select(x => x.ItemPath).ToArray());
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.SelectedItems))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
