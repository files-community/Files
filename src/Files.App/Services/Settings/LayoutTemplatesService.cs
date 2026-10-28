// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Dispatching;

namespace Files.App.Services.Settings
{
	/// <inheritdoc cref="ILayoutTemplatesService"/>
	internal sealed partial class LayoutTemplatesService : ObservableObject, ILayoutTemplatesService
	{
		private static readonly string[] LegacySettingNames =
		[
			"DefaultLayoutMode",
			"DefaultSortOption",
			"DefaultSortColumnId",
			"DefaultDirectorySortDirection",
			"DefaultSortDirectoriesAlongsideFiles",
			"DefaultSortFilesFirst",
			"DefaultGroupOption",
			"DefaultGroupColumnId",
			"DefaultDirectoryGroupDirection",
			"DefaultGroupByDateUnit",
			"DetailsLayoutColumns",
			"DetailsLayoutColumnWidths",
		];

		private const long LegacyAdaptiveLayoutMode = 5;

		private const string PictureAndVideoExtensions = ".jpg, .jpeg, .png, .gif, .bmp, .tif, .tiff, .webp, .heic, .heif, .avif, .jxl, .raw, .dng, .mp4, .mkv, .mov, .avi, .wmv, .webm, .m4v, .mpg, .mpeg, .3gp";
		private const string MusicExtensions = ".mp3, .flac, .wav, .m4a, .aac, .ogg, .opus, .wma, .alac, .aiff";

		private readonly ILayoutSettingsService _settings;
		private readonly DispatcherQueue? _dispatcherQueue;

		private LayoutTemplate[] _templates;
		private LayoutRule[] _rules;

		public IReadOnlyList<LayoutTemplate> Templates
			=> _templates;

		public IReadOnlyList<LayoutRule> Rules
			=> _rules;

		public LayoutTemplate DefaultTemplate
			=> GetTemplate(LayoutTemplateIds.Default) ?? _templates[0];

		public bool HasContentRules
			=> _rules.Any(rule => rule.IsContentRule);

		public event EventHandler<string>? TemplateChanged;

		public event EventHandler? RulesChanged;

		public LayoutTemplatesService(ILayoutSettingsService settings)
		{
			_settings = settings;

			var isFirstRun = false;
			if (settings.LayoutTemplates is { Count: > 0 } templates)
			{
				_templates = templates.Exists(template => template.Id == LayoutTemplateIds.Default)
					? [.. templates]
					: [LayoutTemplateIds.CreateBuiltIn(LayoutTemplateIds.Default), .. templates];
				_rules = settings.LayoutRules is { } rules ? [.. rules] : CreateBuiltInRules(true);
			}
			else
			{
				_templates = [.. LayoutTemplateIds.BuiltIn.Select(LayoutTemplateIds.CreateBuiltIn)];
				_rules = MigrateLegacySettings();
				settings.LayoutTemplates = [.. _templates];
				settings.LayoutRules = [.. _rules];
				isFirstRun = true;
			}

			var includeMediaFolders = _rules.Length > 0;

			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			if (isFirstRun)
				_dispatcherQueue?.TryEnqueue(() => AssignBuiltInTemplates(true, includeMediaFolders));
			Ioc.Default.GetRequiredService<IUserSettingsService>().OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
		}

		public LayoutTemplate? GetTemplate(string? id)
		{
			return string.IsNullOrEmpty(id) ? null : Array.Find(_templates, template => template.Id == id);
		}

		public void SaveTemplate(LayoutTemplate template)
		{
			var index = Array.FindIndex(_templates, item => item.Id == template.Id);
			if (index < 0)
				return;

			var templates = (LayoutTemplate[])_templates.Clone();
			templates[index] = template.Clone();
			SetTemplates(templates);
			TemplateChanged?.Invoke(this, template.Id);
		}

		public LayoutTemplate AddTemplate(LayoutTemplate source, string name)
		{
			var template = source.Clone();
			template.Id = Guid.NewGuid().ToString("N");
			template.Name = GetUniqueName(name);

			SetTemplates([.. _templates, template]);

			return template;
		}

		public void DeleteTemplate(string id)
		{
			if (id == DefaultTemplate.Id || GetTemplate(id) is null)
				return;

			SetTemplates([.. _templates.Where(template => template.Id != id)]);
			SaveRules(_rules.Where(rule => rule.TemplateId != id));
			TemplateChanged?.Invoke(this, id);
		}

		public void ResetTemplate(string id)
		{
			if (LayoutTemplateIds.IsBuiltIn(id) && GetTemplate(id) is { } template)
			{
				var builtIn = LayoutTemplateIds.CreateBuiltIn(id);
				builtIn.Name = template.Name;
				SaveTemplate(builtIn);
			}
		}

		public void SaveRules(IEnumerable<LayoutRule> rules)
		{
			_rules = [.. rules.Select(rule => rule.Clone())];
			_settings.LayoutRules = [.. _rules];
			OnPropertyChanged(nameof(Rules));
			RulesChanged?.Invoke(this, EventArgs.Empty);
		}

		public LayoutTemplate GetTemplateForPath(string? path, Func<LayoutFolderContent?>? getContent = null)
		{
			if (string.IsNullOrEmpty(path))
				return DefaultTemplate;

			// Places are more specific than content, so they're checked first, in template order
			var rules = _templates.SelectMany(template => _rules.Where(rule => rule.TemplateId == template.Id)).ToArray();
			if (rules.FirstOrDefault(rule => !rule.IsContentRule && MatchesPath(rule, path)) is { } pathRule)
				return GetTemplate(pathRule.TemplateId)!;

			if (getContent is null || !rules.Any(rule => rule.IsContentRule) || getContent() is not { } content)
				return DefaultTemplate;

			// The template whose file types fill more of the folder wins
			var contentRule = rules
				.Where(rule => rule.IsContentRule && MatchesContent(rule, content))
				.OrderByDescending(rule => CountMatchingFiles(rule, content))
				.FirstOrDefault();

			return contentRule is not null ? GetTemplate(contentRule.TemplateId)! : DefaultTemplate;
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			// Changes made by another window of the app arrive as JSON
			if (e.NewValue is not JsonElement || e.SettingName is not (nameof(ILayoutSettingsService.LayoutTemplates) or nameof(ILayoutSettingsService.LayoutRules)))
				return;

			void Reload()
			{
				if (e.SettingName is nameof(ILayoutSettingsService.LayoutTemplates) && _settings.LayoutTemplates is { Count: > 0 } templates && templates.Exists(template => template.Id == LayoutTemplateIds.Default))
				{
					var ids = _templates.Select(template => template.Id).Union(templates.Select(template => template.Id)).ToArray();
					_templates = [.. templates];
					OnPropertyChanged(nameof(Templates));
					OnPropertyChanged(nameof(DefaultTemplate));
					foreach (var id in ids)
						TemplateChanged?.Invoke(this, id);
				}
				else if (e.SettingName is nameof(ILayoutSettingsService.LayoutRules))
				{
					_rules = [.. _settings.LayoutRules ?? []];
					OnPropertyChanged(nameof(Rules));
					RulesChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			if (_dispatcherQueue is null || !_dispatcherQueue.TryEnqueue(Reload))
				Reload();
		}

		private static bool MatchesPath(LayoutRule rule, string path)
		{
			switch (rule.Kind)
			{
				case LayoutRuleKind.Folder:
					var folder = LayoutKnownFolders.GetPath(rule.Path).TrimEnd('\\');
					if (string.IsNullOrEmpty(folder))
						return false;

					var trimmedPath = path.TrimEnd('\\');
					return trimmedPath.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
						rule.IncludeSubfolders && trimmedPath.StartsWith(folder + '\\', StringComparison.OrdinalIgnoreCase);
				case LayoutRuleKind.RecycleBin:
					return path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase);
				case LayoutRuleKind.Libraries:
					return LibraryManager.IsLibraryPath(path);
				case LayoutRuleKind.Network:
					return path.StartsWith(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase) ||
						SystemIO.Path.IsPathRooted(path) && DriveHelpers.IsNetworkPath(path);
				default:
					return false;
			}
		}

		private static int CountMatchingFiles(LayoutRule rule, LayoutFolderContent content)
			=> rule.GetExtensions().Sum(extension => content.ExtensionCounts.GetValueOrDefault(extension));

		private static bool MatchesContent(LayoutRule rule, LayoutFolderContent content)
		{
			var count = CountMatchingFiles(rule, content);

			return content.Total > 0 && count * 100 >= rule.Threshold * content.Total;
		}

		private void SetTemplates(LayoutTemplate[] templates)
		{
			_templates = templates;
			_settings.LayoutTemplates = [.. templates];
			OnPropertyChanged(nameof(Templates));
			OnPropertyChanged(nameof(DefaultTemplate));
		}

		private string GetUniqueName(string name)
		{
			var uniqueName = name;
			for (int i = 2; _templates.Any(template => template.DisplayName.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)); i++)
				uniqueName = $"{name} ({i})";

			return uniqueName;
		}

		private LayoutRule[] MigrateLegacySettings()
		{
			var legacySettings = _settings.TakeLegacySettings(LegacySettingNames);
			var defaultTemplate = DefaultTemplate;

			// Grouped by date is the Default template with grouping
			LayoutTemplate[] generalTemplates = [defaultTemplate, .. _templates.Where(template => template.Id is LayoutTemplateIds.GroupedByDate)];
			foreach (var template in generalTemplates)
			{
				if (TryGetNumber("DefaultLayoutMode") is { } layoutMode && layoutMode is not LegacyAdaptiveLayoutMode)
					template.LayoutMode = (FolderLayoutModes)layoutMode;

				template.Columns = TryGetStrings("DetailsLayoutColumns");
				template.ColumnWidths = TryGetStrings("DetailsLayoutColumnWidths");
			}

			foreach (var template in _templates)
			{
				if (TryGetNumber("DefaultSortOption") is { } sortOption)
					template.SortOption = (SortOption)sortOption;
				if (TryGetString("DefaultSortColumnId") is { } sortColumnId)
					template.SortColumnId = sortColumnId;
				if (TryGetNumber("DefaultDirectorySortDirection") is { } sortDirection)
					template.SortDirection = (SortDirection)sortDirection;
				if (TryGetBoolean("DefaultSortDirectoriesAlongsideFiles") is { } sortDirectoriesAlongsideFiles)
					template.SortDirectoriesAlongsideFiles = sortDirectoriesAlongsideFiles;
				if (TryGetBoolean("DefaultSortFilesFirst") is { } sortFilesFirst)
					template.SortFilesFirst = sortFilesFirst;
			}

			if (TryGetNumber("DefaultGroupOption") is { } groupOption)
				defaultTemplate.GroupOption = (GroupOption)groupOption;
			if (TryGetString("DefaultGroupColumnId") is { } groupColumnId)
				defaultTemplate.GroupColumnId = groupColumnId;
			if (TryGetNumber("DefaultDirectoryGroupDirection") is { } groupDirection)
				defaultTemplate.GroupDirection = (SortDirection)groupDirection;
			if (TryGetNumber("DefaultGroupByDateUnit") is { } groupByDateUnit)
				defaultTemplate.GroupByDateUnit = (GroupByDateUnit)groupByDateUnit;

			var isAdaptiveLayoutEnabled = TryGetNumber("DefaultLayoutMode") is null or LegacyAdaptiveLayoutMode;
			return CreateBuiltInRules(isAdaptiveLayoutEnabled);

			long? TryGetNumber(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

			string? TryGetString(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

			bool? TryGetBoolean(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

			List<string>? TryGetStrings(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.Array
					? [.. value.EnumerateArray().Where(item => item.ValueKind is JsonValueKind.String).Select(item => item.GetString()!)]
					: null;
		}

		private static LayoutRule[] CreateBuiltInRules(bool includeMediaRules)
		{
			return includeMediaRules
				?
				[
					new() { Kind = LayoutRuleKind.FileTypes, Extensions = PictureAndVideoExtensions, TemplateId = LayoutTemplateIds.PicturesAndVideos },
					new() { Kind = LayoutRuleKind.FileTypes, Extensions = MusicExtensions, TemplateId = LayoutTemplateIds.Music },
				]
				: [];
		}

		/// <summary>
		/// Makes the matching locations use the built-in templates, as if the user chose them, unless they have their own layout.
		/// </summary>
		private void AssignBuiltInTemplates(bool includeDownloads, bool includeMediaFolders)
		{
			List<(string Path, string TemplateId)> locations = [(Constants.UserEnvironmentPaths.RecycleBinPath, LayoutTemplateIds.RecycleBin)];
			if (includeDownloads)
				locations.Add((LayoutKnownFolders.GetPath(LayoutKnownFolders.Downloads), LayoutTemplateIds.GroupedByDate));
			if (includeMediaFolders)
			{
				locations.Add((LayoutKnownFolders.GetPath(LayoutKnownFolders.Pictures), LayoutTemplateIds.PicturesAndVideos));
				locations.Add((LayoutKnownFolders.GetPath(LayoutKnownFolders.Videos), LayoutTemplateIds.PicturesAndVideos));
			}

			var database = LayoutPreferencesManager.GetDatabaseManagerInstance();
			foreach (var (path, templateId) in locations)
			{
				SafetyExtensions.IgnoreExceptions(() =>
				{
					var trimmedPath = path.TrimPath();
					if (string.IsNullOrEmpty(trimmedPath) || GetTemplate(templateId) is not { } template)
						return;

					var folderFRN = Win32Helper.GetFolderFRN(trimmedPath);
					if (database.GetPreferences(trimmedPath, folderFRN) is null)
						database.SetPreferences(trimmedPath, folderFRN, LayoutPreferencesItem.FromTemplate(template));
				}, App.Logger);
			}
		}
	}
}
