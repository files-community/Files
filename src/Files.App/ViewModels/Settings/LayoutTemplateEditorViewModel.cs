// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Dialogs;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a layout template being edited, which is saved as it changes.
	/// </summary>
	public sealed partial class LayoutTemplateEditorViewModel : ObservableObject
	{
		private readonly ILayoutTemplatesService _templatesService;
		private LayoutTemplate _template;
		private string _name;
		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _catalog;
		private bool _isDeleted;
		private LayoutRule? _fileTypesRule;
		private LayoutRule _lastFileTypesRule = new() { Kind = LayoutRuleKind.FileTypes };

		public bool IsBuiltIn
			=> _template.IsBuiltIn;

		public bool IsDefault
			=> _template.Id == LayoutTemplateIds.Default;

		public bool CanHaveConditions
			=> !IsDefault;

		/// <summary>
		/// Gets whether folders that mostly contain certain file types use this template.
		/// </summary>
		public bool IsUsedForFileTypes
			=> _fileTypesRule is not null;

		/// <summary>
		/// Gets whether the template can be used for file types but isn't yet.
		/// </summary>
		public bool CanTurnOnFileTypes
			=> CanHaveConditions && !IsUsedForFileTypes;

		/// <summary>
		/// Gets or sets the share of files, in percent, that need to have the file types.
		/// </summary>
		public double FileTypesThreshold
		{
			get => _fileTypesRule?.Threshold ?? _lastFileTypesRule.Threshold;
			set
			{
				if (_fileTypesRule is not { } rule || double.IsNaN(value))
				{
					OnPropertyChanged();
					return;
				}

				var threshold = (int)Math.Clamp(Math.Round(value), 1, 100);
				if (threshold != rule.Threshold)
				{
					rule.Threshold = threshold;
					Save();
				}

				OnPropertyChanged();
			}
		}

		public string FileTypesSummary
		{
			get
			{
				if (_fileTypesRule is not { } rule)
					return string.Empty;

				var extensions = rule.GetExtensions().ToArray();
				if (extensions.Length is 0)
					return Strings.None.GetLocalizedResource();

				var shownExtensions = string.Join(", ", extensions.Take(3));
				if (extensions.Length > 3)
					shownExtensions += $" +{extensions.Length - 3}";

				return shownExtensions;
			}
		}

		public string Name
		{
			get => _name;
			set
			{
				if (value != _name)
				{
					_name = value;
					OnPropertyChanged();
					Save();
					OnPropertyChanged(nameof(DeleteName));
					OnPropertyChanged(nameof(RenameName));
					OnPropertyChanged(nameof(DisplayName));
				}
			}
		}

		public string DisplayName
			=> string.IsNullOrWhiteSpace(_name) ? _template.DisplayName : _name.Trim();

		public string RenameName
			=> string.Format(Strings.RenameNamed.GetLocalizedResource(), DisplayName);

		public string DeleteName
			=> string.Format(Strings.DeleteNamed.GetLocalizedResource(), DisplayName);

		public int SelectedLayoutModeIndex
		{
			get => (int)_template.LayoutMode;
			set => Update(SelectedLayoutModeIndex, value, () => _template.LayoutMode = (FolderLayoutModes)value);
		}

		public int SelectedSortDirectionIndex
		{
			get => (int)_template.SortDirection;
			set => Update(SelectedSortDirectionIndex, value, () => _template.SortDirection = (SortDirection)value);
		}

		public int SelectedSortPriorityIndex
		{
			get => _template.SortDirectoriesAlongsideFiles ? 2 : _template.SortFilesFirst ? 1 : 0;
			set => Update(SelectedSortPriorityIndex, value, () =>
			{
				_template.SortDirectoriesAlongsideFiles = value is 2;
				_template.SortFilesFirst = value is 1;
			});
		}

		public int SelectedGroupDirectionIndex
		{
			get => (int)_template.GroupDirection;
			set => Update(SelectedGroupDirectionIndex, value, () => _template.GroupDirection = (SortDirection)value);
		}

		public int SelectedGroupByDateUnitIndex
		{
			get => (int)_template.GroupByDateUnit;
			set => Update(SelectedGroupByDateUnitIndex, value, () => _template.GroupByDateUnit = (GroupByDateUnit)value);
		}

		public bool IsDetailsLayout
			=> _template.LayoutMode is FolderLayoutModes.DetailsView;

		public bool IsGrouped
			=> _template.GroupOption is not GroupOption.None;

		public bool IsGroupByDate
			=> _template.GroupOption.IsGroupByDate();

		public LayoutPropertyPickerViewModel? SortPicker { get; private set; }

		public LayoutPropertyPickerViewModel? GroupPicker { get; private set; }

		public ICommand RestoreDefaultsCommand { get; }

		public ICommand EditFileTypesCommand { get; }

		public LayoutTemplateEditorViewModel(ILayoutTemplatesService templatesService, LayoutTemplate template)
		{
			_templatesService = templatesService;
			_template = template.Clone();
			_name = template.DisplayName;
			_fileTypesRule = templatesService.Rules.FirstOrDefault(rule => rule.TemplateId == template.Id && rule.Kind is LayoutRuleKind.FileTypes)?.Clone();

			RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
			EditFileTypesCommand = new AsyncRelayCommand(EditFileTypesAsync);
		}

		/// <summary>
		/// Saves the template and its rules.
		/// </summary>
		public void Save()
		{
			if (_isDeleted || _templatesService.GetTemplate(_template.Id) is null)
				return;

			var name = _name.Trim();
			if (name.Length > 0)
				_template.Name = _template.IsBuiltIn && name == LayoutTemplateIds.GetBuiltInName(_template.Id) ? string.Empty : name;

			var id = _template.Id;
			_templatesService.SaveTemplate(_template);

			LayoutRule[] conditions = _fileTypesRule is { } rule && rule.GetExtensions().Any() ? [rule] : [];
			foreach (var condition in conditions)
				condition.TemplateId = id;

			_templatesService.SaveRules([.. _templatesService.Rules.Where(rule => rule.TemplateId != id), .. conditions]);
		}

		public void Delete()
		{
			_isDeleted = true;
			_templatesService.DeleteTemplate(_template.Id);
		}

		private void Update<T>(T currentValue, T value, Action update)
		{
			if (value is int index && index < 0 || EqualityComparer<T>.Default.Equals(currentValue, value))
				return;

			update();
			OnPropertyChanged(string.Empty);
			Save();
		}

		/// <summary>
		/// Turns using the template for certain file types on or off.
		/// </summary>
		public void SetUsedForFileTypes(bool value)
		{
			if (value == IsUsedForFileTypes)
				return;

			if (!value)
			{
				_lastFileTypesRule = _fileTypesRule!;
				_fileTypesRule = null;
			}
			else
			{
				_fileTypesRule = _lastFileTypesRule.Clone();
			}

			OnPropertyChanged(nameof(IsUsedForFileTypes));
			OnPropertyChanged(nameof(FileTypesSummary));
			OnPropertyChanged(nameof(CanTurnOnFileTypes));
			OnPropertyChanged(nameof(FileTypesThreshold));
			Save();
		}

		/// <summary>
		/// Finds an extension that another template already uses, so each file type belongs to one template.
		/// </summary>
		private string? FindFileTypeConflict(IEnumerable<string> extensions)
		{
			foreach (var rule in _templatesService.Rules.Where(rule => rule.Kind is LayoutRuleKind.FileTypes && rule.TemplateId != _template.Id))
			{
				var usedExtensions = rule.GetExtensions().ToHashSet(StringComparer.OrdinalIgnoreCase);
				if (extensions.FirstOrDefault(usedExtensions.Contains) is { } extension && _templatesService.GetTemplate(rule.TemplateId) is { } template)
					return string.Format(Strings.FileTypeAlreadyUsed.GetLocalizedResource(), extension, template.DisplayName);
			}

			return null;
		}

		private async Task EditFileTypesAsync()
		{
			if (_fileTypesRule is not { } rule || await LayoutTemplateDialogs.EditFileTypesAsync(rule.Extensions, FindFileTypeConflict) is not { } extensions)
				return;

			rule.Extensions = extensions;
			OnPropertyChanged(nameof(FileTypesSummary));
			Save();
		}

		private void RestoreDefaults()
		{
			_template = LayoutTemplateIds.CreateBuiltIn(_template.Id);
			CreatePickers();
			OnPropertyChanged(string.Empty);
			Save();
		}

		private void CreatePickers()
		{
			var propertyChoices = (_catalog?.Values ?? [])
				.Where(info => !DetailsLayoutColumnDefinitions.IsBuiltIn(info.Name))
				.OrderBy(info => info.DisplayName, StringComparer.CurrentCulture)
				.Select(info => new LayoutPropertyChoice(info.DisplayName, -1, info.Name))
				.ToArray();

			LayoutPropertyChoice[] sortChoices =
			[
				new(Strings.Name.GetLocalizedResource(), (int)SortOption.Name, string.Empty),
				new(Strings.DateModifiedLowerCase.GetLocalizedResource(), (int)SortOption.DateModified, string.Empty),
				new(Strings.DateCreated.GetLocalizedResource(), (int)SortOption.DateCreated, string.Empty),
				new(Strings.Size.GetLocalizedResource(), (int)SortOption.Size, string.Empty),
				new(Strings.Type.GetLocalizedResource(), (int)SortOption.FileType, string.Empty),
				new(Strings.Tag.GetLocalizedResource(), (int)SortOption.FileTag, string.Empty),
				.. propertyChoices,
			];

			LayoutPropertyChoice[] groupChoices =
			[
				new(Strings.None.GetLocalizedResource(), (int)GroupOption.None, string.Empty),
				new(Strings.Name.GetLocalizedResource(), (int)GroupOption.Name, string.Empty),
				new(Strings.DateModifiedLowerCase.GetLocalizedResource(), (int)GroupOption.DateModified, string.Empty),
				new(Strings.DateCreated.GetLocalizedResource(), (int)GroupOption.DateCreated, string.Empty),
				new(Strings.Size.GetLocalizedResource(), (int)GroupOption.Size, string.Empty),
				new(Strings.Type.GetLocalizedResource(), (int)GroupOption.FileType, string.Empty),
				new(Strings.Tag.GetLocalizedResource(), (int)GroupOption.FileTag, string.Empty),
				.. propertyChoices,
			];

			var sortChoice = _template.SortOption is SortOption.Column
				? sortChoices.FirstOrDefault(choice => choice.ColumnId == _template.SortColumnId)
				: sortChoices.FirstOrDefault(choice => choice.Option == (int)_template.SortOption);
			var groupChoice = _template.GroupOption is GroupOption.Column
				? groupChoices.FirstOrDefault(choice => choice.ColumnId == _template.GroupColumnId)
				: groupChoices.FirstOrDefault(choice => choice.Option == (int)_template.GroupOption);

			SortPicker = new(sortChoices, sortChoice, choice =>
			{
				_template.SortOption = choice.Option < 0 ? SortOption.Column : (SortOption)choice.Option;
				_template.SortColumnId = choice.ColumnId;
				Save();
			});

			GroupPicker = new(groupChoices, groupChoice, choice =>
			{
				_template.GroupOption = choice.Option < 0 ? GroupOption.Column : (GroupOption)choice.Option;
				_template.GroupColumnId = choice.ColumnId;
				Save();
				OnPropertyChanged(nameof(IsGrouped));
				OnPropertyChanged(nameof(IsGroupByDate));
			});

			OnPropertyChanged(nameof(SortPicker));
			OnPropertyChanged(nameof(GroupPicker));
		}

		public async Task LoadAsync()
		{
			_catalog = await ShellPropertyCatalog.GetAsync();
			CreatePickers();
		}

		/// <summary>
		/// Creates an editor for the columns of the template, once <see cref="LoadAsync"/> loaded the available columns.
		/// </summary>
		public ColumnCustomizationViewModel? CreateColumnsViewModel()
		{
			if (_catalog is not { } catalog)
				return null;

			var availableColumns = DetailsLayoutColumnIds.All
				.Where(id => id != DetailsLayoutColumnIds.Icon)
				.Concat(catalog.Keys.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)));

			var columns = new ColumnCustomizationViewModel(
				DetailsLayoutColumnDefaults.GetColumns(_template),
				availableColumns,
				DetailsLayoutColumnDefaults.GetOriginalColumns(_template),
				id => DetailsLayoutColumnDefaults.GetDisplayName(id, catalog),
				Strings.CustomizeColumns.GetLocalizedResource());

			columns.Saved += (_, shownColumns) =>
			{
				DetailsLayoutColumnDefaults.SetColumns(_template, shownColumns);
				Save();
			};

			return columns;
		}
	}
}
