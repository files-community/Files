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
		private readonly ILayoutTemplateDialogService _dialogService = Ioc.Default.GetRequiredService<ILayoutTemplateDialogService>();
		private LayoutTemplate _template;
		private string _name;
		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _catalog;
		private bool _isDeleted;
		private LayoutRule? _fileTypesRule;
		private LayoutRule _lastFileTypesRule = new();

		public bool IsBuiltIn
			=> _template.IsBuiltIn;

		public bool IsCustom
			=> !IsBuiltIn;

		public bool IsDefault
			=> _template.Id == LayoutTemplateIds.Default;

		public bool CanHaveConditions
			=> !IsDefault;

		/// <summary>
		/// Gets whether folders that mostly contain certain file types use this template.
		/// </summary>
		public bool IsUsedForFileTypes
		{
			get => _fileTypesRule is not null;
			set => SetUsedForFileTypes(value);
		}

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
					OnPropertyChanged(nameof(Title));
				}
			}
		}

		public string DisplayName
			=> string.IsNullOrWhiteSpace(_name) ? _template.DisplayName : _name.Trim();

		public string Title
			=> IsDefault ? Strings.DefaultLayoutSettings.GetLocalizedResource() : DisplayName;

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
			=> _template.GroupOption.IsGroupByDate() || _template.GroupOption is GroupOption.Property && _template.GroupPropertyId is { } id && LayoutSortGroupOptions.IsDateProperty(id);

		public List<LayoutPropertyChoice> SortChoices { get; private set; } = [];

		public List<LayoutPropertyChoice> GroupChoices { get; private set; } = [];

		public int SelectedSortChoiceIndex
		{
			get => IndexOfChoice(SortChoices, _template.SortOption is SortOption.Property ? null : (int)_template.SortOption, _template.SortPropertyId);
			set => Update(SelectedSortChoiceIndex, value, () =>
			{
				var choice = SortChoices[value];
				_template.SortOption = choice.BuiltInOption is { } option ? (SortOption)option : SortOption.Property;
				_template.SortPropertyId = choice.PropertyId;
			});
		}

		public int SelectedGroupChoiceIndex
		{
			get => IndexOfChoice(GroupChoices, _template.GroupOption is GroupOption.Property ? null : (int)_template.GroupOption, _template.GroupPropertyId);
			set => Update(SelectedGroupChoiceIndex, value, () =>
			{
				var choice = GroupChoices[value];
				_template.GroupOption = choice.BuiltInOption is { } option ? (GroupOption)option : GroupOption.Property;
				_template.GroupPropertyId = choice.PropertyId;
			});
		}

		public ICommand RestoreDefaultsCommand { get; }

		public ICommand EditFileTypesCommand { get; }

		public ICommand RenameCommand { get; }

		public ICommand DeleteCommand { get; }

		public ICommand ManageColumnsCommand { get; }

		/// <summary>
		/// Occurs when the template was deleted.
		/// </summary>
		public event EventHandler? Deleted;

		/// <summary>
		/// Occurs when the user asks to choose the columns of the template.
		/// </summary>
		public event EventHandler<ColumnCustomizationViewModel>? ManageColumnsRequested;

		public LayoutTemplateEditorViewModel(ILayoutTemplatesService templatesService, LayoutTemplate template)
		{
			_templatesService = templatesService;
			_template = template.Clone();
			_name = template.DisplayName;
			_fileTypesRule = templatesService.Rules.FirstOrDefault(rule => rule.TemplateId == template.Id)?.Clone();

			RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
			EditFileTypesCommand = new AsyncRelayCommand(EditFileTypesAsync);
			RenameCommand = new AsyncRelayCommand(RenameAsync);
			DeleteCommand = new AsyncRelayCommand(DeleteAsync);
			ManageColumnsCommand = new RelayCommand(ManageColumns);
		}

		/// <summary>
		/// Saves the template and its rules.
		/// </summary>
		public void Save()
		{
			if (_isDeleted || _templatesService.GetTemplate(_template.Id) is null)
				return;

			var name = _name.Trim();
			if (!_template.IsBuiltIn && name.Length > 0)
				_template.Name = name;

			var id = _template.Id;
			_templatesService.SaveTemplate(_template);

			LayoutRule[] conditions = _fileTypesRule is { } rule && rule.GetExtensions().Any() ? [rule] : [];
			foreach (var condition in conditions)
				condition.TemplateId = id;

			var savedConditions = _templatesService.Rules.Where(rule => rule.TemplateId == id).ToArray();
			if (savedConditions.Length == conditions.Length && savedConditions.Zip(conditions).All(pair => pair.First.Extensions == pair.Second.Extensions && pair.First.Threshold == pair.Second.Threshold))
				return;

			_templatesService.SaveRules(_templatesService.Rules.Where(rule => rule.TemplateId != id).Concat(conditions).ToArray());
		}

		private async Task RenameAsync()
		{
			if (await _dialogService.AskNameAsync(Strings.RenameTemplate.GetLocalizedResource(), Strings.Rename.GetLocalizedResource(), DisplayName) is { } name)
				Name = name;
		}

		private async Task DeleteAsync()
		{
			if (!await _dialogService.ConfirmDeleteAsync(DisplayName))
				return;

			_isDeleted = true;
			_templatesService.DeleteTemplate(_template.Id);
			Deleted?.Invoke(this, EventArgs.Empty);
		}

		private void ManageColumns()
		{
			if (CreateColumnsViewModel() is { } columns)
				ManageColumnsRequested?.Invoke(this, columns);
		}

		private void Update<T>(T currentValue, T value, Action update)
		{
			if (value is int index && index < 0 || EqualityComparer<T>.Default.Equals(currentValue, value))
				return;

			update();
			OnPropertyChanged(string.Empty);
			Save();
		}

		private void SetUsedForFileTypes(bool value)
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
			foreach (var rule in _templatesService.Rules.Where(rule => rule.TemplateId != _template.Id))
			{
				var usedExtensions = rule.GetExtensions().ToHashSet(StringComparer.OrdinalIgnoreCase);
				if (extensions.FirstOrDefault(usedExtensions.Contains) is { } extension && _templatesService.GetTemplate(rule.TemplateId) is { } template)
					return string.Format(Strings.FileTypeAlreadyUsed.GetLocalizedResource(), extension, template.DisplayName);
			}

			return null;
		}

		private async Task EditFileTypesAsync()
		{
			if (_fileTypesRule is not { } rule || await _dialogService.EditFileTypesAsync(rule.Extensions, FindFileTypeConflict) is not { } extensions)
				return;

			rule.Extensions = extensions;
			OnPropertyChanged(nameof(FileTypesSummary));
			Save();
		}

		private void RestoreDefaults()
		{
			_template = LayoutTemplateIds.CreateBuiltIn(_template.Id);
			_fileTypesRule = _templatesService.GetBuiltInRule(_template.Id)?.Clone();
			if (_fileTypesRule is { } rule)
			{
				// File types another template uses stay with it
				var usedExtensions = _templatesService.Rules.Where(other => other.TemplateId != _template.Id).SelectMany(other => other.GetExtensions()).ToHashSet(StringComparer.OrdinalIgnoreCase);
				rule.Extensions = string.Join(", ", rule.GetExtensions().Where(extension => !usedExtensions.Contains(extension)));
			}

			UpdateChoices();
			OnPropertyChanged(string.Empty);
			Save();
		}

		private void UpdateChoices()
		{
			var shellPropertyChoices = DetailsLayoutColumnDefaults.GetColumns(_template)
				.Append(_template.SortPropertyId)
				.Append(_template.GroupPropertyId)
				.Where(ColumnValueHelper.IsShellPropertyColumn)
				.Distinct()
				.Select(id => new LayoutPropertyChoice(DetailsLayoutColumnDefaults.GetDisplayName(id, _catalog), null, id))
				.OrderBy(choice => choice.DisplayName, StringComparer.CurrentCulture)
				.ToArray();

			SortChoices =
			[
				new(Strings.Name.GetLocalizedResource(), (int)SortOption.Name, string.Empty),
				new(Strings.DateModifiedLowerCase.GetLocalizedResource(), (int)SortOption.DateModified, string.Empty),
				new(Strings.DateCreated.GetLocalizedResource(), (int)SortOption.DateCreated, string.Empty),
				new(Strings.Size.GetLocalizedResource(), (int)SortOption.Size, string.Empty),
				new(Strings.Type.GetLocalizedResource(), (int)SortOption.FileType, string.Empty),
				new(Strings.Tag.GetLocalizedResource(), (int)SortOption.FileTag, string.Empty),
				.. shellPropertyChoices,
			];

			GroupChoices =
			[
				new(Strings.None.GetLocalizedResource(), (int)GroupOption.None, string.Empty),
				new(Strings.Name.GetLocalizedResource(), (int)GroupOption.Name, string.Empty),
				new(Strings.DateModifiedLowerCase.GetLocalizedResource(), (int)GroupOption.DateModified, string.Empty),
				new(Strings.DateCreated.GetLocalizedResource(), (int)GroupOption.DateCreated, string.Empty),
				new(Strings.Size.GetLocalizedResource(), (int)GroupOption.Size, string.Empty),
				new(Strings.Type.GetLocalizedResource(), (int)GroupOption.FileType, string.Empty),
				new(Strings.Tag.GetLocalizedResource(), (int)GroupOption.FileTag, string.Empty),
				.. shellPropertyChoices,
			];

			OnPropertyChanged(nameof(SortChoices));
			OnPropertyChanged(nameof(GroupChoices));
			OnPropertyChanged(nameof(SelectedSortChoiceIndex));
			OnPropertyChanged(nameof(SelectedGroupChoiceIndex));
		}

		private static int IndexOfChoice(IReadOnlyList<LayoutPropertyChoice> choices, int? builtInOption, string? propertyId)
		{
			for (int i = 0; i < choices.Count; i++)
			{
				if (builtInOption is null ? choices[i].PropertyId == propertyId : choices[i].BuiltInOption == builtInOption)
					return i;
			}

			return -1;
		}

		public async Task LoadAsync()
		{
			_catalog = await ShellPropertyCatalog.GetAsync();
			UpdateChoices();
		}

		/// <summary>
		/// Creates an editor for the columns of the template, once <see cref="LoadAsync"/> loaded the available columns.
		/// </summary>
		private ColumnCustomizationViewModel? CreateColumnsViewModel()
		{
			if (_catalog is not { } catalog)
				return null;

			var availableColumns = DetailsLayoutColumnIds.BuiltIn
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
				UpdateChoices();
				Save();
			};

			return columns;
		}
	}
}
