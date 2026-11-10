// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Dialogs;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SaveLayoutAsTemplateAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly ILayoutTemplatesService templatesService;
		private readonly ILayoutTemplateDialogService dialogService = Ioc.Default.GetRequiredService<ILayoutTemplateDialogService>();
		private LayoutPreferencesManager? folderSettings;

		public string Label
			=> Strings.SaveLayoutAsTemplate.GetLocalizedResource();

		public string Description
			=> Strings.SaveLayoutAsTemplateDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public bool IsExecutable
			=> folderSettings is not null && LayoutHelpers.GetAppliedTemplate(folderSettings) is null;

		public SaveLayoutAsTemplateAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			templatesService = Ioc.Default.GetRequiredService<ILayoutTemplatesService>();
			context.PropertyChanged += Context_PropertyChanged;
			templatesService.PropertyChanged += TemplatesService_PropertyChanged;
			UpdateFolderSettings();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var shellPage = context.ShellPage;
			var folderName = shellPage?.ShellViewModel?.CurrentFolder?.Name;
			var name = await dialogService.AskNameAsync(
				Strings.SaveLayoutAsTemplate.GetLocalizedResource(),
				Strings.Save.GetLocalizedResource(),
				string.IsNullOrEmpty(folderName) ? Strings.NewTemplate.GetLocalizedResource() : folderName);

			if (name is not null)
				LayoutHelpers.ChangeFolderPreferences(shellPage, path => LayoutPreferencesManager.SaveAsNewTemplateFromPath(path, name));
		}

		private void UpdateFolderSettings()
		{
			if (folderSettings is not null)
				folderSettings.PropertyChanged -= FolderSettings_PropertyChanged;

			folderSettings = context.ShellPage?.InstanceViewModel?.FolderSettings;
			if (folderSettings is not null)
				folderSettings.PropertyChanged += FolderSettings_PropertyChanged;

			OnPropertyChanged(nameof(IsExecutable));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage))
				UpdateFolderSettings();
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(LayoutPreferencesManager.TemplateId) or nameof(LayoutPreferencesManager.IsCustomized))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void TemplatesService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ILayoutTemplatesService.Templates))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
