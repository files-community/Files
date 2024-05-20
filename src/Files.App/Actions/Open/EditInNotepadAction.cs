// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class EditInNotepadAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "EditInNotepad".GetLocalizedResource();

		public string Description
			=> "EditInNotepadDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE70F");

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			FileExtensionHelpers.IsBatchFile(context.SelectedItem.FileExtension);

		public EditInNotepadAction()
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
