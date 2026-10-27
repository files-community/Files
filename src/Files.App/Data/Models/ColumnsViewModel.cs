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
		private const double DefaultPropertyColumnWidth = 150;

		private readonly Dictionary<string, DetailsLayoutColumnItem> _columns = [];
		private readonly List<string> _propertyColumnIds = [];

		public ColumnsViewModel()
		{
			foreach (var definition in DetailsLayoutColumnDefinitions.All)
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

				foreach (var id in _propertyColumnIds)
					_columns.Remove(id);

				_propertyColumnIds.Clear();
				foreach (var entry in value)
				{
					var parts = entry.Split('|');
					var id = parts[0];
					if (string.IsNullOrEmpty(id) || id == DetailsLayoutColumnIds.Icon)
						continue;

					var isVisible = parts.Length < 3 || parts[2] != "0";
					double width = 0;
					var hasWidth = parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out width);

					if (_columns.TryGetValue(id, out var column))
					{
						if (_propertyColumnIds.Contains(id))
							continue;

						if (hasWidth)
							column.UserLengthPixels = width;

						column.UserCollapsed = !isVisible && !DetailsLayoutColumnOrder.IsFixed(id);
					}
					else if (isVisible)
					{
						column = CreatePropertyColumn();
						column.UserLengthPixels = hasWidth ? width : DefaultPropertyColumnWidth;
						_columns[id] = column;
						_propertyColumnIds.Add(id);
					}
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
		public DetailsLayoutColumnOrder OrderedColumns => _OrderedColumns ??= new(_ColumnOrder, _propertyColumnIds, this);

		[RegistryIgnore, JsonIgnore]
		public IReadOnlyList<string> PropertyColumnIds => _propertyColumnIds;

		[RegistryIgnore, JsonIgnore]
		public IEnumerable<string> AllColumnIds => DetailsLayoutColumnIds.All.Concat(_propertyColumnIds);

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

		public DetailsLayoutColumnItem? FindColumn(string id)
		{
			return _columns.GetValueOrDefault(id);
		}

		public bool HasPropertyColumn(string id)
		{
			return _propertyColumnIds.Contains(id);
		}

		public bool IsColumnVisible(string id)
		{
			return _columns.TryGetValue(id, out var column) && !column.UserCollapsed;
		}

		public void SetColumnVisibility(string id, bool isVisible, double defaultWidth = DefaultPropertyColumnWidth)
		{
			if (DetailsLayoutColumnOrder.IsFixed(id))
				return;

			if (DetailsLayoutColumnDefinitions.IsBuiltIn(id))
			{
				GetColumn(id).UserCollapsed = !isVisible;
				return;
			}

			if (isVisible == HasPropertyColumn(id))
				return;

			if (isVisible)
			{
				var column = CreatePropertyColumn();
				column.UserLengthPixels = defaultWidth;
				_columns[id] = column;
				_propertyColumnIds.Add(id);
			}
			else
			{
				_columns.Remove(id);
				_propertyColumnIds.Remove(id);
			}

			OnColumnSetChanged();
		}

		private static DetailsLayoutColumnItem CreatePropertyColumn()
		{
			return new() { NormalMaxLength = 1000 };
		}

		private void OnColumnSetChanged()
		{
			_OrderedColumns = null;
			OnPropertyChanged(nameof(Columns));
			OnPropertyChanged(nameof(OrderedColumns));
		}

		public void MoveColumn(string id, string targetId, bool after)
		{
			if (id == targetId || DetailsLayoutColumnOrder.IsFixed(id))
				return;

			if (DetailsLayoutColumnOrder.IsFixed(targetId))
				(targetId, after) = (DetailsLayoutColumnIds.Name, true);

			var ids = OrderedColumns.Ids.ToList();
			ids.Remove(id);

			var targetIndex = ids.IndexOf(targetId);
			if (targetIndex < 0)
				return;

			ids.Insert(after ? targetIndex + 1 : targetIndex, id);
			ColumnOrder = [.. ids.Where(x => !DetailsLayoutColumnOrder.IsFixed(x))];
		}

		public void MoveColumn(string id, int direction)
		{
			if (FindVisibleNeighbor(id, direction) is { } neighborId)
				MoveColumn(id, neighborId, direction > 0);
		}

		public bool CanMoveColumn(string id, int direction)
		{
			return FindVisibleNeighbor(id, direction) is not null;
		}

		private string? FindVisibleNeighbor(string id, int direction)
		{
			var ids = OrderedColumns.Ids;
			var index = OrderedColumns.IndexOf(id);
			if (index < 0 || DetailsLayoutColumnOrder.IsFixed(id))
				return null;

			for (int i = index + direction; i >= 0 && i < ids.Count && !DetailsLayoutColumnOrder.IsFixed(ids[i]); i += direction)
			{
				if (GetColumn(ids[i]).Visibility == Visibility.Visible)
					return ids[i];
			}

			return null;
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
