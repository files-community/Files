// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a group or a column in the available columns of <see cref="Views.Settings.ColumnCustomizationPage"/>.
	/// </summary>
	public sealed partial class ColumnTreeItem(string id, string displayName, bool isGroup) : ObservableObject
	{
		public string Id { get; } = id;

		public string DisplayName { get; } = displayName;

		public bool IsGroup { get; } = isGroup;

		public ObservableCollection<ColumnTreeItem> Children { get; } = [];

		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set => SetProperty(ref _IsExpanded, value);
		}
	}

	/// <summary>
	/// Represents a shown column in <see cref="Views.Settings.ColumnCustomizationPage"/>.
	/// </summary>
	public sealed record ColumnListItem(string Id, string DisplayName)
	{
		/// <summary>
		/// Gets whether the column always stays first and can't be removed.
		/// </summary>
		public bool IsPinned => DetailsLayoutColumnOrder.IsFixed(Id);

		public bool IsMovable => !IsPinned;
	}

	/// <summary>
	/// Edits which columns of <see cref="DetailsLayoutPage"/> are shown and in which order.
	/// </summary>
	public sealed partial class ColumnCustomizationViewModel : ObservableObject
	{
		private readonly IReadOnlyList<string> _availableColumns;
		private readonly IReadOnlyList<string> _defaultColumns;
		private readonly Func<string, string> _getDisplayName;

		public ObservableCollection<ColumnTreeItem> AvailableColumnGroups { get; } = [];

		public ObservableCollection<ColumnListItem> ShownColumns { get; } = [];

		private string _SearchQuery = string.Empty;
		public string SearchQuery
		{
			get => _SearchQuery;
			set
			{
				if (SetProperty(ref _SearchQuery, value ?? string.Empty))
					UpdateAvailableColumns();
			}
		}

		/// <summary>
		/// Occurs when the user saves the shown columns, in display order.
		/// </summary>
		public event EventHandler<IReadOnlyList<string>>? Saved;

		public string Title { get; }

		public string? Description { get; }

		public bool HasDescription => !string.IsNullOrEmpty(Description);

		public ColumnCustomizationViewModel(IEnumerable<string> shownColumns, IEnumerable<string> availableColumns, IEnumerable<string> defaultColumns, Func<string, string> getDisplayName, string title, string? description = null)
		{
			_availableColumns = [.. availableColumns.Where(id => !DetailsLayoutColumnOrder.IsFixed(id)).Distinct()];
			_defaultColumns = [.. defaultColumns];
			_getDisplayName = getDisplayName;
			Title = title;
			Description = description;

			SetShownColumns(shownColumns);
			ShownColumns.CollectionChanged += (_, _) => UpdateAvailableColumns();
			UpdateAvailableColumns();
		}

		/// <summary>
		/// Adds the specified columns at the specified position, after the pinned column.
		/// </summary>
		public void AddColumns(IEnumerable<string> ids, int index = -1)
		{
			var insertIndex = index < 0 || index > ShownColumns.Count ? ShownColumns.Count : Math.Max(index, 1);
			foreach (var id in ids)
			{
				if (ShownColumns.Any(column => column.Id == id))
					continue;

				ShownColumns.Insert(insertIndex++, new(id, _getDisplayName(id)));
			}
		}

		public void RemoveColumn(ColumnListItem column)
		{
			if (!column.IsPinned)
				ShownColumns.Remove(column);
		}

		/// <summary>
		/// Moves the pinned column back to the top after the user reordered the list.
		/// </summary>
		public void KeepPinnedColumnFirst()
		{
			if (ShownColumns.FirstOrDefault(column => column.IsPinned) is { } pinnedColumn && ShownColumns.IndexOf(pinnedColumn) is > 0 and var index)
				ShownColumns.Move(index, 0);
		}

		public void RestoreDefaults()
		{
			SetShownColumns(_defaultColumns);
		}

		public void Save()
		{
			Saved?.Invoke(this, [.. ShownColumns.Where(column => !column.IsPinned).Select(column => column.Id)]);
		}

		private void SetShownColumns(IEnumerable<string> columns)
		{
			ShownColumns.Clear();
			ShownColumns.Add(new(DetailsLayoutColumnIds.Name, _getDisplayName(DetailsLayoutColumnIds.Name)));
			foreach (var id in columns.Where(id => !DetailsLayoutColumnOrder.IsFixed(id)).Distinct())
				ShownColumns.Add(new(id, _getDisplayName(id)));
		}

		private void UpdateAvailableColumns()
		{
			var query = SearchQuery.Trim();
			var shownIds = ShownColumns.Select(column => column.Id).ToHashSet();
			var expandedGroups = AvailableColumnGroups.Where(group => group.IsExpanded).Select(group => group.Id).ToHashSet();
			var groups = _availableColumns
				.Where(id => !shownIds.Contains(id))
				.Select(id => (Id: id, DisplayName: _getDisplayName(id)))
				.Where(column => query.Length == 0 || column.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
				.GroupBy(column => GetGroup(column.Id))
				.OrderBy(group => group.Key);

			AvailableColumnGroups.Clear();
			foreach (var group in groups)
			{
				var groupItem = new ColumnTreeItem(group.Key.ToString(), GetGroupDisplayName(group.Key), true) { IsExpanded = query.Length > 0 || expandedGroups.Contains(group.Key.ToString()) };
				foreach (var (id, displayName) in group.OrderBy(column => column.DisplayName, StringComparer.CurrentCultureIgnoreCase))
					groupItem.Children.Add(new(id, displayName, false));

				AvailableColumnGroups.Add(groupItem);
			}
		}

		private enum ColumnGroup
		{
			General,
			Git,
			Music,
			Pictures,
			Videos,
			Documents,
			Other,
		}

		private static ColumnGroup GetGroup(string id)
		{
			if (DetailsLayoutColumnDefinitions.Get(id) is { } definition)
				return definition.IsGitColumn ? ColumnGroup.Git : ColumnGroup.General;

			var parts = id.Split('.');
			if (parts.Length < 3)
				return ColumnGroup.General;

			return parts[1] switch
			{
				"Music" or "Audio" => ColumnGroup.Music,
				"Photo" or "Image" or "GPS" => ColumnGroup.Pictures,
				"Video" or "Media" or "RecordedTV" => ColumnGroup.Videos,
				"Document" => ColumnGroup.Documents,
				_ => ColumnGroup.Other,
			};
		}

		private static string GetGroupDisplayName(ColumnGroup group)
		{
			return (group switch
			{
				ColumnGroup.General => Strings.General,
				ColumnGroup.Git => Strings.Git,
				ColumnGroup.Music => Strings.Music,
				ColumnGroup.Pictures => Strings.Pictures,
				ColumnGroup.Videos => Strings.Videos,
				ColumnGroup.Documents => Strings.Documents,
				_ => Strings.OtherColumns,
			}).GetLocalizedResource();
		}
	}
}
