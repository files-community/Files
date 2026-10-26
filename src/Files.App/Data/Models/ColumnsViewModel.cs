// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	[WinRT.GeneratedBindableCustomProperty(
		[
			nameof(IconColumn),
			nameof(GitStatusColumn),
			nameof(GitLastCommitDateColumn),
			nameof(GitLastCommitMessageColumn),
			nameof(GitCommitAuthorColumn),
			nameof(GitLastCommitShaColumn),
			nameof(TagColumn),
			nameof(NameColumn),
			nameof(StatusColumn),
			nameof(DateModifiedColumn),
			nameof(PathColumn),
			nameof(OriginalPathColumn),
			nameof(ItemTypeColumn),
			nameof(DateDeletedColumn),
			nameof(DateCreatedColumn),
			nameof(SizeColumn),
			nameof(OrderedColumns),
		],
		[])]
	public sealed partial class ColumnsViewModel : ObservableObject
	{
		private DetailsLayoutColumnItem iconColumn = new()
		{
			UserLength = new GridLength(24, GridUnitType.Pixel),
			IsResizable = false,
		};

		[RegistryIgnore]
		public DetailsLayoutColumnItem IconColumn
		{
			get => iconColumn;
			set => SetProperty(ref iconColumn, value);
		}

		private DetailsLayoutColumnItem _GitStatusColumn = new();
		public DetailsLayoutColumnItem GitStatusColumn
		{
			get => _GitStatusColumn;
			set => SetProperty(ref _GitStatusColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitDateColumn = new();
		public DetailsLayoutColumnItem GitLastCommitDateColumn
		{
			get => _GitLastCommitDateColumn;
			set => SetProperty(ref _GitLastCommitDateColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitMessageColumn = new();
		public DetailsLayoutColumnItem GitLastCommitMessageColumn
		{
			get => _GitLastCommitMessageColumn;
			set => SetProperty(ref _GitLastCommitMessageColumn, value);
		}

		private DetailsLayoutColumnItem _GitCommitAuthorColumn = new();
		public DetailsLayoutColumnItem GitCommitAuthorColumn
		{
			get => _GitCommitAuthorColumn;
			set => SetProperty(ref _GitCommitAuthorColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitShaColumn = new();
		public DetailsLayoutColumnItem GitLastCommitShaColumn
		{
			get => _GitLastCommitShaColumn;
			set => SetProperty(ref _GitLastCommitShaColumn, value);
		}

		private DetailsLayoutColumnItem tagColumn = new();
		public DetailsLayoutColumnItem TagColumn
		{
			get => tagColumn;
			set => SetProperty(ref tagColumn, value);
		}

		private DetailsLayoutColumnItem nameColumn = new()
		{
			NormalMaxLength = 1000d
		};
		public DetailsLayoutColumnItem NameColumn
		{
			get => nameColumn;
			set => SetProperty(ref nameColumn, value);
		}

		private DetailsLayoutColumnItem statusColumn = new()
		{
			UserLength = new GridLength(50),
			NormalMaxLength = 80,
		};
		public DetailsLayoutColumnItem StatusColumn
		{
			get => statusColumn;
			set => SetProperty(ref statusColumn, value);
		}

		private DetailsLayoutColumnItem dateModifiedColumn = new();
		public DetailsLayoutColumnItem DateModifiedColumn
		{
			get => dateModifiedColumn;
			set => SetProperty(ref dateModifiedColumn, value);
		}

		private DetailsLayoutColumnItem pathColumn = new()
		{
			NormalMaxLength = 500,
		};
		public DetailsLayoutColumnItem PathColumn
		{
			get => pathColumn;
			set => SetProperty(ref pathColumn, value);
		}

		private DetailsLayoutColumnItem originalPathColumn = new()
		{
			NormalMaxLength = 500,
		};
		public DetailsLayoutColumnItem OriginalPathColumn
		{
			get => originalPathColumn;
			set => SetProperty(ref originalPathColumn, value);
		}

		private DetailsLayoutColumnItem itemTypeColumn = new();
		public DetailsLayoutColumnItem ItemTypeColumn
		{
			get => itemTypeColumn;
			set => SetProperty(ref itemTypeColumn, value);
		}

		private DetailsLayoutColumnItem dateDeletedColumn = new();
		public DetailsLayoutColumnItem DateDeletedColumn
		{
			get => dateDeletedColumn;
			set => SetProperty(ref dateDeletedColumn, value);
		}

		private DetailsLayoutColumnItem dateCreatedColumn = new()
		{
			UserCollapsed = true
		};
		public DetailsLayoutColumnItem DateCreatedColumn
		{
			get => dateCreatedColumn;
			set => SetProperty(ref dateCreatedColumn, value);
		}

		private DetailsLayoutColumnItem sizeColumn = new();
		public DetailsLayoutColumnItem SizeColumn
		{
			get => sizeColumn;
			set => SetProperty(ref sizeColumn, value);
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
		public DetailsLayoutColumnOrder OrderedColumns => _OrderedColumns ??= new(_ColumnOrder);

		public void MoveColumn(string id, string targetId, bool after)
		{
			if (id == targetId || id == DetailsLayoutColumnIds.Icon || targetId == DetailsLayoutColumnIds.Icon)
				return;

			var ids = OrderedColumns.Ids.ToList();
			ids.Remove(id);

			var targetIndex = ids.IndexOf(targetId);
			if (targetIndex < 0)
				return;

			ids.Insert(after ? targetIndex + 1 : targetIndex, id);
			ColumnOrder = [.. ids.Where(x => x != DetailsLayoutColumnIds.Icon)];
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
			if (index <= 0)
				return null;

			for (int i = index + direction; i > 0 && i < ids.Count; i += direction)
			{
				if (GetColumn(ids[i]).Visibility == Visibility.Visible)
					return ids[i];
			}

			return null;
		}

		public double TotalWidth =>
			DetailsLayoutColumnIds.All.Sum(id => GetColumn(id).Length.Value);

		public DetailsLayoutColumnItem GetColumn(string id)
		{
			return id switch
			{
				DetailsLayoutColumnIds.Icon => IconColumn,
				DetailsLayoutColumnIds.Name => NameColumn,
				DetailsLayoutColumnIds.GitStatus => GitStatusColumn,
				DetailsLayoutColumnIds.GitLastCommitDate => GitLastCommitDateColumn,
				DetailsLayoutColumnIds.GitLastCommitMessage => GitLastCommitMessageColumn,
				DetailsLayoutColumnIds.GitCommitAuthor => GitCommitAuthorColumn,
				DetailsLayoutColumnIds.GitLastCommitSha => GitLastCommitShaColumn,
				DetailsLayoutColumnIds.Tag => TagColumn,
				DetailsLayoutColumnIds.Path => PathColumn,
				DetailsLayoutColumnIds.OriginalPath => OriginalPathColumn,
				DetailsLayoutColumnIds.DateDeleted => DateDeletedColumn,
				DetailsLayoutColumnIds.DateModified => DateModifiedColumn,
				DetailsLayoutColumnIds.DateCreated => DateCreatedColumn,
				DetailsLayoutColumnIds.ItemType => ItemTypeColumn,
				DetailsLayoutColumnIds.Size => SizeColumn,
				DetailsLayoutColumnIds.Status => StatusColumn,
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
			};
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnsViewModel model)
			{
				foreach (var id in DetailsLayoutColumnIds.All)
				{
					if (id != DetailsLayoutColumnIds.Icon && !model.GetColumn(id).Equals(GetColumn(id)))
						return false;
				}

				return model.OrderedColumns.Ids.SequenceEqual(OrderedColumns.Ids);
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = 0;
			foreach (var id in DetailsLayoutColumnIds.All)
			{
				if (id != DetailsLayoutColumnIds.Icon)
					hashCode = (hashCode * 397) ^ GetColumn(id).GetHashCode();
			}

			foreach (var id in OrderedColumns.Ids)
				hashCode = (hashCode * 397) ^ id.GetHashCode();

			return hashCode;
		}
	}
}
