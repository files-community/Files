// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System.Globalization;

namespace Files.App.Data.Models
{
	[WinRT.GeneratedBindableCustomProperty(
		[
			nameof(IconColumn),
			nameof(NameColumn),
			nameof(GitStatusColumn),
			nameof(TagColumn),
			nameof(SizeColumn),
			nameof(StatusColumn),
			nameof(OrderedColumns),
			nameof(LayoutVersion),
		],
		[])]
	public sealed partial class ColumnsViewModel : ObservableObject
	{

		private readonly Dictionary<string, DetailsLayoutColumnItem> _columns = [];

		public ColumnsViewModel()
		{
			foreach (var definition in DetailsLayoutColumnDefinitions.BuiltIn)
			{
				_columns[definition.Id] = new()
				{
					UserLengthPixels = definition.DefaultWidth,
					NormalMinLength = definition.Id == DetailsLayoutColumnIds.Icon ? 0 : definition.MinWidth,
					NormalMaxLength = definition.MaxWidth,
					IsResizable = definition.IsResizable,
					UserCollapsed = !definition.IsVisibleByDefault,
				};
			}
		}

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem IconColumn => _columns[DetailsLayoutColumnIds.Icon];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem NameColumn => _columns[DetailsLayoutColumnIds.Name];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem GitStatusColumn => _columns[DetailsLayoutColumnIds.GitStatus];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem TagColumn => _columns[DetailsLayoutColumnIds.Tag];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem SizeColumn => _columns[DetailsLayoutColumnIds.Size];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem StatusColumn => _columns[DetailsLayoutColumnIds.Status];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem GitLastCommitDateColumn => _columns[DetailsLayoutColumnIds.GitLastCommitDate];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem GitLastCommitMessageColumn => _columns[DetailsLayoutColumnIds.GitLastCommitMessage];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem GitCommitAuthorColumn => _columns[DetailsLayoutColumnIds.GitCommitAuthor];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem GitLastCommitShaColumn => _columns[DetailsLayoutColumnIds.GitLastCommitSha];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem PathColumn => _columns[DetailsLayoutColumnIds.Path];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem OriginalPathColumn => _columns[DetailsLayoutColumnIds.OriginalPath];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem DateDeletedColumn => _columns[DetailsLayoutColumnIds.DateDeleted];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem DateModifiedColumn => _columns[DetailsLayoutColumnIds.DateModified];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem DateCreatedColumn => _columns[DetailsLayoutColumnIds.DateCreated];

		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnItem ItemTypeColumn => _columns[DetailsLayoutColumnIds.ItemType];

		/// <summary>
		/// Reads a column saved before the <see cref="Columns"/> format.
		/// </summary>
		private void SetLegacyColumn(string id, DetailsLayoutColumnItem value)
		{
			var column = _columns[id];
			column.UserLengthPixels = value.UserLengthPixels;
			column.UserCollapsed = value.UserCollapsed && !DetailsLayoutColumnOrder.IsRequired(id);
		}

		[RegistryIgnore, JsonPropertyName("IconColumn")]
		public DetailsLayoutColumnItem LegacyIconColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Icon, value); }

		[RegistryIgnore, JsonPropertyName("NameColumn")]
		public DetailsLayoutColumnItem LegacyNameColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Name, value); }

		[RegistryIgnore, JsonPropertyName("GitStatusColumn")]
		public DetailsLayoutColumnItem LegacyGitStatusColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.GitStatus, value); }

		[RegistryIgnore, JsonPropertyName("GitLastCommitDateColumn")]
		public DetailsLayoutColumnItem LegacyGitLastCommitDateColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.GitLastCommitDate, value); }

		[RegistryIgnore, JsonPropertyName("GitLastCommitMessageColumn")]
		public DetailsLayoutColumnItem LegacyGitLastCommitMessageColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.GitLastCommitMessage, value); }

		[RegistryIgnore, JsonPropertyName("GitCommitAuthorColumn")]
		public DetailsLayoutColumnItem LegacyGitCommitAuthorColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.GitCommitAuthor, value); }

		[RegistryIgnore, JsonPropertyName("GitLastCommitShaColumn")]
		public DetailsLayoutColumnItem LegacyGitLastCommitShaColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.GitLastCommitSha, value); }

		[RegistryIgnore, JsonPropertyName("TagColumn")]
		public DetailsLayoutColumnItem LegacyTagColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Tag, value); }

		[RegistryIgnore, JsonPropertyName("PathColumn")]
		public DetailsLayoutColumnItem LegacyPathColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Path, value); }

		[RegistryIgnore, JsonPropertyName("OriginalPathColumn")]
		public DetailsLayoutColumnItem LegacyOriginalPathColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.OriginalPath, value); }

		[RegistryIgnore, JsonPropertyName("DateDeletedColumn")]
		public DetailsLayoutColumnItem LegacyDateDeletedColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.DateDeleted, value); }

		[RegistryIgnore, JsonPropertyName("DateModifiedColumn")]
		public DetailsLayoutColumnItem LegacyDateModifiedColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.DateModified, value); }

		[RegistryIgnore, JsonPropertyName("DateCreatedColumn")]
		public DetailsLayoutColumnItem LegacyDateCreatedColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.DateCreated, value); }

		[RegistryIgnore, JsonPropertyName("ItemTypeColumn")]
		public DetailsLayoutColumnItem LegacyItemTypeColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.ItemType, value); }

		[RegistryIgnore, JsonPropertyName("SizeColumn")]
		public DetailsLayoutColumnItem LegacySizeColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Size, value); }

		[RegistryIgnore, JsonPropertyName("StatusColumn")]
		public DetailsLayoutColumnItem LegacyStatusColumn { set => SetLegacyColumn(DetailsLayoutColumnIds.Status, value); }

		/// <summary>
		/// Gets or sets the column widths and visibility as "id|width|visible" entries.
		/// </summary>
		public string[] Columns
		{
			get => [.. AllColumnIds
				.Where(id => id != DetailsLayoutColumnIds.Icon)
				.Select(id => $"{id}|{_columns[id].UserLengthPixels.ToString(CultureInfo.InvariantCulture)}|{(_columns[id].UserCollapsed ? 0 : 1)}")];
			set
			{
				value ??= [];
				if (Columns.SequenceEqual(value))
					return;

				foreach (var entry in value)
				{
					var parts = entry.Split('|');
					var id = parts[0];
					if (string.IsNullOrEmpty(id) || id == DetailsLayoutColumnIds.Icon)
						continue;

					var isVisible = parts.Length < 3 || parts[2] != "0";
					double width = 0;
					var hasWidth = parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out width);

					if (!_columns.TryGetValue(id, out var column))
						continue;

					if (hasWidth)
						column.UserLengthPixels = width;

					column.UserCollapsed = !isVisible && !DetailsLayoutColumnOrder.IsRequired(id);
				}

				OnColumnSetChanged();
			}
		}

		private string[] _ColumnOrder = [];
		public string[] ColumnOrder
		{
			get => _ColumnOrder;
			set
			{
				value ??= [];
				if (_ColumnOrder.SequenceEqual(value))
					return;

				_ColumnOrder = value;
				_OrderedColumns = null;
				OnPropertyChanged(nameof(ColumnOrder));
				OnPropertyChanged(nameof(OrderedColumns));
			}
		}

		private DetailsLayoutColumnOrder? _OrderedColumns;
		[RegistryIgnore, JsonIgnore]
		public DetailsLayoutColumnOrder OrderedColumns => _OrderedColumns ??= new(_ColumnOrder, this);

		[RegistryIgnore, JsonIgnore]
		public IEnumerable<string> AllColumnIds => DetailsLayoutColumnIds.BuiltIn;

		/// <summary>
		/// Gets a value that changes whenever the width or visibility of a column changes.
		/// </summary>
		[RegistryIgnore, JsonIgnore]
		public int LayoutVersion { get; private set; }

		public void NotifyLayoutChanged()
		{
			LayoutVersion++;
			OnPropertyChanged(nameof(LayoutVersion));
		}

		public DetailsLayoutColumnItem GetColumn(string id)
		{
			return _columns.TryGetValue(id, out var column) ? column : throw new ArgumentOutOfRangeException(nameof(id), id, null);
		}

		/// <summary>
		/// Uses the widths of the specified columns for the columns both have.
		/// </summary>
		public void ApplyWidths(ColumnsViewModel source)
		{
			foreach (var id in AllColumnIds)
			{
				if (source.FindColumn(id) is { } column)
					GetColumn(id).UserLengthPixels = column.UserLengthPixels;
			}
		}

		public DetailsLayoutColumnItem? FindColumn(string id)
		{
			return _columns.GetValueOrDefault(id);
		}

		public bool IsColumnVisible(string id)
		{
			return _columns.TryGetValue(id, out var column) && !column.UserCollapsed;
		}

		public void SetColumnVisibility(string id, bool isVisible)
		{
			if (!DetailsLayoutColumnOrder.IsRequired(id))
				GetColumn(id).UserCollapsed = !isVisible;
		}

		private void OnColumnSetChanged()
		{
			_OrderedColumns = null;
			OnPropertyChanged(nameof(Columns));
			OnPropertyChanged(nameof(OrderedColumns));
		}

		public void MoveColumn(string id, string targetId, bool after)
		{
			if (id == targetId || DetailsLayoutColumnOrder.IsPinned(id))
				return;

			var ids = OrderedColumns.Ids.Where(x => !DetailsLayoutColumnOrder.IsPinned(x)).ToList();
			ids.Remove(id);

			var targetIndex = DetailsLayoutColumnOrder.IsPinned(targetId) ? 0 : ids.IndexOf(targetId);
			if (targetIndex < 0)
				return;

			ids.Insert(after && !DetailsLayoutColumnOrder.IsPinned(targetId) ? targetIndex + 1 : targetIndex, id);
			ColumnOrder = [.. ids];
		}

		/// <summary>
		/// Gets the columns for the column menu in display order.
		/// </summary>
		public IEnumerable<string> GetMenuColumnIds()
		{
			return OrderedColumns.Ids.Where(id => !DetailsLayoutColumnOrder.IsRequired(id) && !GetColumn(id).IsHidden);
		}


		public double TotalWidth =>
			AllColumnIds.Sum(id => GetColumn(id).Length.Value);

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnsViewModel model)
				return model.Columns.SequenceEqual(Columns) && model.OrderedColumns.Ids.SequenceEqual(OrderedColumns.Ids);

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = 0;
			foreach (var column in Columns)
				hashCode = (hashCode * 397) ^ column.GetHashCode();

			foreach (var id in OrderedColumns.Ids)
				hashCode = (hashCode * 397) ^ id.GetHashCode();

			return hashCode;
		}
	}
}
