// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Windows.Win32;

namespace Files.App.Services.Settings
{
	/// <inheritdoc cref="ILayoutTemplatesService"/>
	internal sealed partial class LayoutTemplatesService : ObservableObject, ILayoutTemplatesService
	{
		private static readonly string[] LegacySettingNames =
		[
			"DefaultLayoutMode",
			"DefaultSortOption",
			"DefaultDirectorySortDirection",
			"DefaultSortDirectoriesAlongsideFiles",
			"DefaultSortFilesFirst",
			"DefaultGroupOption",
			"DefaultDirectoryGroupDirection",
			"DefaultGroupByDateUnit",
			"SyncFolderPreferencesAcrossDirectories",
		];

		private const long LegacyAdaptiveLayoutMode = 5;

		private static readonly string PictureAndVideoExtensions = string.Join(", ", FileExtensionHelpers.PictureExtensions.Concat(FileExtensionHelpers.VideoExtensions));
		private static readonly string MusicExtensions = string.Join(", ", FileExtensionHelpers.AudioExtensions);

		private readonly ILayoutSettingsService _settings;
		private readonly DispatcherQueue? _dispatcherQueue;

		private LayoutTemplate[] _templates;
		private LayoutRule[] _rules;
		private bool _clearFolderPreferences;
		private (LayoutTemplate Template, int Threshold, string[] Extensions)[]? _ruleMatchers;
		private volatile bool _isFirstRunPending;
		private readonly Lock _firstRunLock = new();

		public IReadOnlyList<LayoutTemplate> Templates
			=> _templates;

		public IReadOnlyList<LayoutRule> Rules
			=> _rules;

		public LayoutTemplate DefaultTemplate
			=> GetTemplate(LayoutTemplateIds.Default) ?? _templates[0];

		public bool HasRules
			=> _rules.Length > 0;

		public event EventHandler<string>? TemplateChanged;

		public event EventHandler? RulesChanged;

		public LayoutTemplatesService(ILayoutSettingsService settings)
		{
			_settings = settings;

			if (settings.LayoutTemplates is { Count: > 0 } templates)
			{
				_templates = templates.Exists(template => template.Id == LayoutTemplateIds.Default)
					? [.. templates]
					: [LayoutTemplateIds.CreateBuiltIn(LayoutTemplateIds.Default), .. templates];
				_rules = settings.LayoutRules is { } rules ? [.. rules] : CreateBuiltInRules(true);
			}
			else if (settings.HasSavedLayoutTemplates)
			{
				// Saved templates that can't be read aren't overwritten
				_templates = [.. LayoutTemplateIds.BuiltIn.Select(LayoutTemplateIds.CreateBuiltIn)];
				_rules = settings.LayoutRules is { } rules ? [.. rules] : CreateBuiltInRules(true);
			}
			else
			{
				_templates = [.. LayoutTemplateIds.BuiltIn.Select(LayoutTemplateIds.CreateBuiltIn)];
				_rules = MigrateLegacySettings();
				settings.LayoutTemplates = [.. _templates];
				settings.LayoutRules = [.. _rules];
				_isFirstRunPending = true;
			}

			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			if (_isFirstRunPending)
				_dispatcherQueue?.TryEnqueue(CompleteFirstRun);
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
			do
				template.Id = Guid.NewGuid().ToString("N");
			while (GetTemplate(template.Id) is not null);

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

		public LayoutRule? GetBuiltInRule(string templateId)
			=> CreateBuiltInRules(true).FirstOrDefault(rule => rule.TemplateId == templateId);

		public void SaveRules(IEnumerable<LayoutRule> rules)
		{
			_rules = [.. rules.Select(rule => rule.Clone())];
			_ruleMatchers = null;
			_settings.LayoutRules = [.. _rules];
			OnPropertyChanged(nameof(Rules));
			RulesChanged?.Invoke(this, EventArgs.Empty);
		}

		public LayoutTemplate GetTemplateForPath(string? path, Func<LayoutFolderContent?>? getContent = null)
		{
			if (string.IsNullOrEmpty(path))
				return DefaultTemplate;

			var matchers = _ruleMatchers ??= [.. _templates.SelectMany(template => _rules
				.Where(rule => rule.TemplateId == template.Id)
				.Select(rule => (template, rule.Threshold, rule.GetExtensions().ToArray())))];
			if (getContent is null || matchers.Length == 0 || getContent() is not { Total: > 0 } content)
				return DefaultTemplate;

			// The template whose file types fill more of the folder wins
			var bestTemplate = DefaultTemplate;
			var bestCount = 0;
			foreach (var (template, threshold, extensions) in matchers)
			{
				var count = extensions.Sum(extension => content.ExtensionCounts.GetValueOrDefault(extension));
				if (count > bestCount && count * 100 >= threshold * content.Total)
				{
					bestTemplate = template;
					bestCount = count;
				}
			}

			return bestTemplate;
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
					_ruleMatchers = null;
					OnPropertyChanged(nameof(Templates));
					OnPropertyChanged(nameof(DefaultTemplate));
					foreach (var id in ids)
						TemplateChanged?.Invoke(this, id);
				}
				else if (e.SettingName is nameof(ILayoutSettingsService.LayoutRules))
				{
					_rules = [.. _settings.LayoutRules ?? []];
					_ruleMatchers = null;
					OnPropertyChanged(nameof(Rules));
					RulesChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			if (_dispatcherQueue is null || !_dispatcherQueue.TryEnqueue(Reload))
				Reload();
		}

		private void SetTemplates(LayoutTemplate[] templates)
		{
			_templates = templates;
			_ruleMatchers = null;
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

		// TODO: Remove in v5
		public void MigrateImportedSettings()
		{
			if (_settings.HasSavedLayoutTemplates)
				return;

			var ids = _templates.Select(template => template.Id).Union(LayoutTemplateIds.BuiltIn).ToArray();
			_templates = [.. LayoutTemplateIds.BuiltIn.Select(LayoutTemplateIds.CreateBuiltIn)];
			_ruleMatchers = null;
			var rules = MigrateLegacySettings();
			SetTemplates(_templates);
			SaveRules(rules);
			foreach (var id in ids)
				TemplateChanged?.Invoke(this, id);

			_isFirstRunPending = true;
			CompleteFirstRun();
		}

		// TODO: Remove in v5
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
			}

			foreach (var template in _templates)
			{
				if (TryGetNumber("DefaultSortOption") is { } sortOption)
					template.SortOption = (SortOption)sortOption;
				if (TryGetNumber("DefaultDirectorySortDirection") is { } sortDirection)
					template.SortDirection = (SortDirection)sortDirection;
				if (TryGetBoolean("DefaultSortDirectoriesAlongsideFiles") is { } sortDirectoriesAlongsideFiles)
					template.SortDirectoriesAlongsideFiles = sortDirectoriesAlongsideFiles;
				if (TryGetBoolean("DefaultSortFilesFirst") is { } sortFilesFirst)
					template.SortFilesFirst = sortFilesFirst;
			}

			if (TryGetNumber("DefaultGroupOption") is { } groupOption)
				defaultTemplate.GroupOption = (GroupOption)groupOption;
			if (TryGetNumber("DefaultDirectoryGroupDirection") is { } groupDirection)
				defaultTemplate.GroupDirection = (SortDirection)groupDirection;
			if (TryGetNumber("DefaultGroupByDateUnit") is { } groupByDateUnit)
				defaultTemplate.GroupByDateUnit = (GroupByDateUnit)groupByDateUnit;

			_clearFolderPreferences = TryGetBoolean("SyncFolderPreferencesAcrossDirectories") is true;

			var isAdaptiveLayoutEnabled = TryGetNumber("DefaultLayoutMode") is null or LegacyAdaptiveLayoutMode;
			return CreateBuiltInRules(isAdaptiveLayoutEnabled);

			long? TryGetNumber(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

			bool? TryGetBoolean(string name)
				=> legacySettings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
		}

		private static LayoutRule[] CreateBuiltInRules(bool includeMediaRules)
		{
			return includeMediaRules
				?
				[
					new() { Extensions = PictureAndVideoExtensions, TemplateId = LayoutTemplateIds.PicturesAndVideos },
					new() { Extensions = MusicExtensions, TemplateId = LayoutTemplateIds.Music },
				]
				: [];
		}

		public string? GetBuiltInTemplateId(string path)
		{
			var trimmedPath = path.TrimPath();
			return GetBuiltInLocations().FirstOrDefault(location => string.Equals(location.Path.TrimPath(), trimmedPath, StringComparison.OrdinalIgnoreCase)).TemplateId;
		}

		private static (string Path, string TemplateId)[] GetBuiltInLocations()
		{
			return
			[
				(Constants.UserEnvironmentPaths.RecycleBinPath, LayoutTemplateIds.RecycleBin),
				(Win32Helper.GetFolderFromKnownFolderGUID(PInvoke.FOLDERID_Downloads), LayoutTemplateIds.GroupedByDate),
				(Win32Helper.GetFolderFromKnownFolderGUID(PInvoke.FOLDERID_Pictures), LayoutTemplateIds.PicturesAndVideos),
				(Win32Helper.GetFolderFromKnownFolderGUID(PInvoke.FOLDERID_Videos), LayoutTemplateIds.PicturesAndVideos),
			];
		}

		public void CompleteFirstRun()
		{
			if (!_isFirstRunPending)
				return;

			lock (_firstRunLock)
			{
				if (!_isFirstRunPending)
					return;

				if (_clearFolderPreferences)
					SafetyExtensions.IgnoreExceptions(LayoutPreferencesManager.GetDatabaseManagerInstance().ResetAll, App.Logger);

				AssignBuiltInTemplates();
				_isFirstRunPending = false;
			}
		}

		/// <summary>
		/// Makes the matching locations use the built-in templates, as if the user chose them, unless they have their own layout.
		/// </summary>
		private void AssignBuiltInTemplates()
		{
			var database = LayoutPreferencesManager.GetDatabaseManagerInstance();
			foreach (var (path, templateId) in GetBuiltInLocations())
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
