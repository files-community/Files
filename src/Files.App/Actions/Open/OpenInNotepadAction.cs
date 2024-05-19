// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class OpenInNotepadAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenInNotepad".GetLocalizedResource();

		public string Description
			=> "OpenInNotepadDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE756");

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			FileExtensionHelpers.IsBatchFile(context.SelectedItem.FileExtension);

		public OpenInNotepadAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.RunPowershellCommandAsync($"notepad '{context.ShellPage?.SlimContentPage?.SelectedItem?.ItemPath}\'", false);

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
