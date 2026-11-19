// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a group or a column in <see cref="Views.Settings.ColumnCustomizationPage"/>.
	/// </summary>
	public sealed partial class ColumnPickerItem : ObservableObject
	{
		public ColumnPickerItem(string id, string displayName, ColumnCustomizationViewModel owner, ColumnPickerItem? group = null)
		{
			Id = id;
			DisplayName = displayName;
			Group = group;
			ToggleCommand = new RelayCommand(() => owner.Toggle(this));
			ActivateCommand = new RelayCommand(() =>
			{
				if (IsGroup)
					owner.ToggleExpanded(this);
				else
					owner.Toggle(this);
			});
		}

		/// <summary>
		/// Gets the command that shows or hides the column.
		/// </summary>
		public ICommand ToggleCommand { get; }

		/// <summary>
		/// Gets the command that runs when the row is clicked: a group expands or collapses, and a column is shown or hidden.
		/// </summary>
		public ICommand ActivateCommand { get; }

		public string Id { get; }

		public string DisplayName { get; }

		/// <summary>
		/// Gets the group of the column, or null when the item is a group.
		/// </summary>
		public ColumnPickerItem? Group { get; }

		public bool IsGroup
			=> Group is null;

		public bool IsColumn
			=> !IsGroup;

		/// <summary>
		/// Gets whether the user can change the item, which isn't the case for the column that is always shown.
		/// </summary>
		public bool IsEnabled
			=> !DetailsLayoutColumnOrder.IsRequired(Id);

		public List<ColumnPickerItem> Columns { get; } = [];

		private bool _IsChecked;
		/// <summary>
		/// Gets whether the column is shown.
		/// </summary>
		public bool IsChecked
		{
			get => _IsChecked;
			set => SetProperty(ref _IsChecked, value);
		}

		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set
			{
				if (SetProperty(ref _IsExpanded, value))
					OnPropertyChanged(nameof(ExpandGlyphAngle));
			}
		}

		public double ExpandGlyphAngle
			=> IsExpanded ? 90 : 0;

		public override string ToString()
			=> DisplayName;
	}

	/// <summary>
	/// Represents a shown column in <see cref="Views.Settings.ColumnCustomizationPage"/>, in display order.
	/// </summary>
	public sealed partial class ShownColumnItem : ObservableObject
	{
		private readonly ColumnCustomizationViewModel _owner;

		public ShownColumnItem(string id, string displayName, ColumnCustomizationViewModel owner)
		{
			Id = id;
			DisplayName = displayName;
			_owner = owner;
			RemoveCommand = new RelayCommand(() => owner.Remove(this));
		}

		public string Id { get; }

		public string DisplayName { get; }

		/// <summary>
		/// Gets whether the column can be hidden, which isn't the case for the column that is always shown.
		/// </summary>
		public bool IsRemovable
			=> !DetailsLayoutColumnOrder.IsRequired(Id);

		/// <summary>
		/// Gets or sets the direction items are sorted by the column, or -1 when they are sorted by another column.
		/// </summary>
		public int SortDirectionIndex
		{
			get => _owner.SortColumnId == Id ? (int)_owner.SortDirection : -1;
			set
			{
				if (value >= 0 && value != SortDirectionIndex)
					_owner.SetSort(Id, (SortDirection)value);
			}
		}

		public ICommand RemoveCommand { get; }

		internal void NotifySortChanged()
			=> OnPropertyChanged(nameof(SortDirectionIndex));

		public override string ToString()
			=> DisplayName;
	}

	/// <summary>
	/// Represents what the user saved in <see cref="Views.Settings.ColumnCustomizationPage"/>.
	/// </summary>
	/// <param name="Columns">The shown columns, in display order.</param>
	/// <param name="SortColumnId">The column the items are sorted by, or null to keep the current sort.</param>
	public sealed record ColumnCustomizationResult(IReadOnlyList<string> Columns, string? SortColumnId, SortDirection SortDirection);

	/// <summary>
	/// Edits which columns of <see cref="DetailsLayoutPage"/> are shown, their order and the column the items are sorted by.
	/// </summary>
	public sealed partial class ColumnCustomizationViewModel : ObservableObject
	{
		private readonly IReadOnlyList<string> _defaultColumns;
		private readonly List<string> _shownColumns = [];
		private readonly IReadOnlyList<ColumnPickerItem> _groups;
		private readonly Func<string, string> _getDisplayName;
		private bool _isSyncingShownColumns;
		private ColumnsViewModel? _columns;
		private LayoutPreferencesManager? _folderSettings;
		private string[] _availableColumns = [];
		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _catalog;

		/// <summary>
		/// Gets the groups and the columns of the expanded groups, in display order.
		/// </summary>
		public ObservableCollection<ColumnPickerItem> Items { get; } = [];

		/// <summary>
		/// Gets the shown columns in display order. Reordering the collection reorders the columns.
		/// </summary>
		public ObservableCollection<ShownColumnItem> ShownColumns { get; } = [];

		/// <summary>
		/// Gets the column the items are sorted by, or null when the sort column isn't shown.
		/// </summary>
		public string? SortColumnId { get; private set; }

		public SortDirection SortDirection { get; private set; }

		private string _SearchQuery = string.Empty;
		public string SearchQuery
		{
			get => _SearchQuery;
			set
			{
				if (SetProperty(ref _SearchQuery, value ?? string.Empty))
					UpdateItems();
			}
		}

		/// <summary>
		/// Occurs when the user saves the columns and the sort.
		/// </summary>
		public event EventHandler<ColumnCustomizationResult>? Saved;

		/// <summary>
		/// Occurs when the window should close, after saving or canceling.
		/// </summary>
		public event EventHandler? CloseRequested;

		public ICommand RestoreDefaultsCommand { get; }

		public ICommand SaveCommand { get; }

		public ICommand CancelCommand { get; }

		public string Title { get; }

		public string? Description { get; }

		public bool HasDescription => !string.IsNullOrEmpty(Description);

		/// <param name="sortColumnId">The column the items are currently sorted by.</param>
		public ColumnCustomizationViewModel(IEnumerable<string> shownColumns, IEnumerable<string> availableColumns, IEnumerable<string> defaultColumns, Func<string, string> getDisplayName, string? sortColumnId, SortDirection sortDirection, string title, string? description = null)
		{
			_defaultColumns = defaultColumns.ToArray();
			_getDisplayName = getDisplayName;
			SortColumnId = sortColumnId;
			SortDirection = sortDirection;
			Title = title;
			Description = description;

			var columnIds = new[] { DetailsLayoutColumnIds.Name }.Concat(availableColumns.Where(id => !id.StartsWith("System.VersionControl.", StringComparison.Ordinal))).Concat(shownColumns).Where(id => !DetailsLayoutColumnOrder.IsPinned(id)).Distinct().ToArray();
			_groups = columnIds
				.GroupBy(DetailsLayoutColumnGroups.GetGroup)
				.OrderBy(group => group.Key)
				.Select(group =>
				{
					var groupItem = new ColumnPickerItem(group.Key.ToString(), DetailsLayoutColumnGroups.GetDisplayName(group.Key), this);
					groupItem.Columns.AddRange(group
						.Select(id => new ColumnPickerItem(id, getDisplayName(id), this, groupItem))
						.OrderBy(column => column.IsEnabled)
						.ThenBy(column => column.DisplayName, StringComparer.CurrentCultureIgnoreCase));

					return groupItem;
				}).ToArray();

			RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
			SaveCommand = new RelayCommand(Save);
			CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

			ShownColumns.CollectionChanged += ShownColumns_CollectionChanged;
			SetShownColumns(shownColumns);
			UpdateItems();
		}

		/// <summary>
		/// Creates the editor for the columns of a folder, with the columns its location offers.
		/// </summary>
		public static async Task<ColumnCustomizationViewModel> CreateForFolderAsync(ColumnsViewModel columns, LayoutPreferencesManager folderSettings, string? path, Func<string, string> getDisplayName)
		{
			var catalog = await ShellPropertyCatalog.GetAsync();
			var folderColumns = await ShellFolderColumns.GetAsync(path);
			var (availableColumns, shownColumns) = columns.GetCustomizationColumns(folderColumns, catalog);
			var defaults = new LayoutPreferencesItem().ColumnsViewModel;
			var defaultColumns = defaults.OrderedColumns.Ids.Where(id => defaults.IsColumnVisible(id) && (DetailsLayoutColumnOrder.IsRequired(id) || availableColumns.Contains(id)));
			var sortColumnId = GetSortColumnId(folderSettings.DirectorySortOption, folderSettings.DirectorySortPropertyId);

			return new(shownColumns, availableColumns, defaultColumns, getDisplayName, sortColumnId, folderSettings.DirectorySortDirection, Strings.CustomizeColumns.GetLocalizedResource(), Strings.CustomizeColumnsDescription.GetLocalizedResource())
			{
				_columns = columns,
				_folderSettings = folderSettings,
				_availableColumns = availableColumns,
				_catalog = catalog,
			};
		}

		/// <summary>
		/// Gets the column that the specified sort option sorts by, or null when it doesn't sort by a column.
		/// </summary>
		public static string? GetSortColumnId(SortOption sortOption, string? sortPropertyId)
		{
			return sortOption is SortOption.Property
				? sortPropertyId
				: DetailsLayoutColumnDefinitions.BuiltIn.FirstOrDefault(definition => definition.SortOption == sortOption)?.Id;
		}

		/// <summary>
		/// Applies the saved columns and sort to the folder the editor was created for.
		/// </summary>
		/// <returns>Whether a column was added, whose values may need loading.</returns>
		public bool ApplyToFolder(ColumnCustomizationResult result)
		{
			var hasNewColumns = _columns?.ApplyCustomization(result.Columns, _availableColumns, _catalog) is true;
			if (_folderSettings is { } folderSettings && result.SortColumnId is { } sortColumnId)
			{
				if (DetailsLayoutColumnDefinitions.Get(sortColumnId)?.SortOption is { } sortOption)
					folderSettings.DirectorySortOption = sortOption;
				else
					folderSettings.SetSortProperty(sortColumnId);

				folderSettings.DirectorySortDirection = result.SortDirection;
			}

			return hasNewColumns;
		}

		/// <summary>
		/// Shows or hides the specified column.
		/// </summary>
		public void Toggle(ColumnPickerItem item)
		{
			if (item.IsGroup || !item.IsEnabled)
				return;

			SetColumnShown(item, !item.IsChecked);
			UpdateCheckStates();
			SyncShownColumns();
		}

		/// <summary>
		/// Hides the specified column.
		/// </summary>
		public void Remove(ShownColumnItem item)
		{
			if (!item.IsRemovable || !_shownColumns.Remove(item.Id))
				return;

			UpdateCheckStates();
			SyncShownColumns();
		}

		/// <summary>
		/// Sorts the items by the specified column.
		/// </summary>
		public void SetSort(string columnId, SortDirection direction)
		{
			SortColumnId = columnId;
			SortDirection = direction;
			OnPropertyChanged(nameof(SortColumnId));
			OnPropertyChanged(nameof(SortDirection));
			foreach (var item in ShownColumns)
				item.NotifySortChanged();
		}

		/// <summary>
		/// Moves the specified column by the specified number of positions.
		/// </summary>
		public void Move(ShownColumnItem item, int offset)
		{
			var index = ShownColumns.IndexOf(item);
			var newIndex = Math.Clamp(index + offset, 0, ShownColumns.Count - 1);
			if (index < 0 || newIndex == index)
				return;

			ShownColumns.RemoveAt(index);
			ShownColumns.Insert(newIndex, item);
		}

		private void RestoreDefaults()
		{
			SetShownColumns(_defaultColumns);
		}

		private void Save()
		{
			Saved?.Invoke(this, new(_shownColumns.ToArray(), SortColumnId, SortDirection));
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		private void ShownColumns_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (_isSyncingShownColumns)
				return;

			_shownColumns.Clear();
			_shownColumns.AddRange(ShownColumns.Select(item => item.Id));
		}

		private void SyncShownColumns()
		{
			_isSyncingShownColumns = true;
			try
			{
				for (var i = ShownColumns.Count - 1; i >= 0; i--)
				{
					if (!_shownColumns.Contains(ShownColumns[i].Id))
						ShownColumns.RemoveAt(i);
				}

				for (var i = 0; i < _shownColumns.Count; i++)
				{
					var id = _shownColumns[i];
					var index = ShownColumns.ToList().FindIndex(item => item.Id == id);
					if (index < 0)
						ShownColumns.Insert(i, new(id, _getDisplayName(id), this));
					else if (index != i)
					{
						var item = ShownColumns[index];
						ShownColumns.RemoveAt(index);
						ShownColumns.Insert(i, item);
					}
				}
			}
			finally
			{
				_isSyncingShownColumns = false;
			}

		}

		private void SetColumnShown(ColumnPickerItem column, bool isShown)
		{
			if (!isShown)
				_shownColumns.Remove(column.Id);
			else if (!_shownColumns.Contains(column.Id))
				_shownColumns.Add(column.Id);
		}

		private void SetShownColumns(IEnumerable<string> columns)
		{
			_shownColumns.Clear();
			_shownColumns.AddRange(columns.Where(id => !DetailsLayoutColumnOrder.IsPinned(id)).Distinct());
			UpdateCheckStates();
			SyncShownColumns();
		}

		private void UpdateCheckStates()
		{
			foreach (var group in _groups)
			{
				foreach (var column in group.Columns)
					column.IsChecked = !column.IsEnabled || _shownColumns.Contains(column.Id);
			}
		}

		private void UpdateItems()
		{
			var query = SearchQuery.Trim();
			var items = new List<ColumnPickerItem>();
			foreach (var group in _groups)
			{
				var columns = group.Columns.Where(column => query.Length == 0 || column.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
				if (columns.Length == 0)
					continue;

				if (query.Length > 0)
					group.IsExpanded = true;

				items.Add(group);
				if (group.IsExpanded)
					items.AddRange(columns);
			}

			var itemSet = items.ToHashSet();
			for (int i = Items.Count - 1; i >= 0; i--)
			{
				if (!itemSet.Contains(Items[i]))
					Items.RemoveAt(i);
			}

			for (int i = 0; i < items.Count; i++)
			{
				if (i >= Items.Count || Items[i] != items[i])
					Items.Insert(i, items[i]);
			}
		}

		/// <summary>
		/// Expands or collapses the specified group.
		/// </summary>
		public void ToggleExpanded(ColumnPickerItem group)
		{
			group.IsExpanded = !group.IsExpanded;
			UpdateItems();
		}
	}
}
