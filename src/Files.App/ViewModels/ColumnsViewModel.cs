using Microsoft.UI.Xaml;

namespace Files.App.ViewModels
{
	public class ColumnsViewModel : ObservableObject
	{
		private ColumnViewModel iconColumn = new()
		{
			UserLength = new GridLength(24, GridUnitType.Pixel),
			IsResizeable = false,
		};

		[LiteDB.BsonIgnore]
		public ColumnViewModel IconColumn
		{
			get => iconColumn;
			set => SetProperty(ref iconColumn, value);
		}

		private ColumnViewModel tagColumn = new();
		public ColumnViewModel TagColumn
		{
			get => tagColumn;
			set => SetProperty(ref tagColumn, value);
		}

		private ColumnViewModel nameColumn = new()
		{
			NormalMaxLength = 1000d
		};

		public ColumnViewModel NameColumn
		{
			get => nameColumn;
			set => SetProperty(ref nameColumn, value);
		}

		private ColumnViewModel statusColumn = new()
		{
			UserLength = new GridLength(50),
			NormalMaxLength = 80,
		};

		public ColumnViewModel StatusColumn
		{
			get => statusColumn;
			set => SetProperty(ref statusColumn, value);
		}

		private ColumnViewModel dateModifiedColumn = new();
		public ColumnViewModel DateModifiedColumn
		{
			get => dateModifiedColumn;
			set => SetProperty(ref dateModifiedColumn, value);
		}

		private ColumnViewModel originalPathColumn = new()
		{
			NormalMaxLength = 500,
		};

		public ColumnViewModel OriginalPathColumn
		{
			get => originalPathColumn;
			set => SetProperty(ref originalPathColumn, value);
		}

		private ColumnViewModel itemTypeColumn = new();
		public ColumnViewModel ItemTypeColumn
		{
			get => itemTypeColumn;
			set => SetProperty(ref itemTypeColumn, value);
		}

		private ColumnViewModel dateDeletedColumn = new();
		public ColumnViewModel DateDeletedColumn
		{
			get => dateDeletedColumn;
			set => SetProperty(ref dateDeletedColumn, value);
		}

		private ColumnViewModel dateCreatedColumn = new()
		{
			UserCollapsed = true
		};

		public ColumnViewModel DateCreatedColumn
		{
			get => dateCreatedColumn;
			set => SetProperty(ref dateCreatedColumn, value);
		}

		private ColumnViewModel sizeColumn = new();
		public ColumnViewModel SizeColumn
		{
			get => sizeColumn;
			set => SetProperty(ref sizeColumn, value);
		}

		[LiteDB.BsonIgnore]
		public double TotalWidth => IconColumn.Length.Value + TagColumn.Length.Value + NameColumn.Length.Value + DateModifiedColumn.Length.Value + OriginalPathColumn.Length.Value
			+ ItemTypeColumn.Length.Value + DateDeletedColumn.Length.Value + DateCreatedColumn.Length.Value + SizeColumn.Length.Value + StatusColumn.Length.Value;

		public void SetDesiredSize(double width)
		{
			if (TotalWidth > width || TotalWidth < width)
			{
				var proportion = width / TotalWidth;

				//SetColumnSizeProportionally(proportion);
			}
		}

		/// <summary>
		/// Multiplies every column's length by the given amount if it is within the size
		/// </summary>
		/// <param name="factor"></param>
		private void SetColumnSizeProportionally(double factor)
		{
			NameColumn.TryMultiplySize(factor);
			TagColumn.TryMultiplySize(factor);
			DateModifiedColumn.TryMultiplySize(factor);
			OriginalPathColumn.TryMultiplySize(factor);
			ItemTypeColumn.TryMultiplySize(factor);
			DateDeletedColumn.TryMultiplySize(factor);
			DateCreatedColumn.TryMultiplySize(factor);
			SizeColumn.TryMultiplySize(factor);
			StatusColumn.TryMultiplySize(factor);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnsViewModel model)
			{
				return (
					model.DateCreatedColumn.Equals(DateCreatedColumn) &&
					model.DateDeletedColumn.Equals(DateDeletedColumn) &&
					model.DateModifiedColumn.Equals(DateModifiedColumn) &&
					model.ItemTypeColumn.Equals(ItemTypeColumn) &&
					model.NameColumn.Equals(NameColumn) &&
					model.OriginalPathColumn.Equals(OriginalPathColumn) &&
					model.SizeColumn.Equals(SizeColumn) &&
					model.StatusColumn.Equals(StatusColumn) &&
					model.TagColumn.Equals(TagColumn));
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = DateCreatedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ DateDeletedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ DateModifiedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ ItemTypeColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ NameColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ OriginalPathColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ SizeColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ StatusColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ TagColumn.GetHashCode();

			return hashCode;
		}
	}

	public class ColumnViewModel : ObservableObject
	{
		private bool isHidden;

		[LiteDB.BsonIgnore]
		public bool IsHidden
		{
			get => isHidden;
			set => SetProperty(ref isHidden, value);
		}

		[LiteDB.BsonIgnore]
		public double MaxLength
		{
			get => UserCollapsed ? 0 : NormalMaxLength;
		}

		private double normalMaxLength = 800;

		public double NormalMaxLength
		{
			get => normalMaxLength;
			set => SetProperty(ref normalMaxLength, value);
		}

		private double normalMinLength = 50;

		[LiteDB.BsonIgnore]
		public double NormalMinLength
		{
			get => normalMinLength;
			set
			{
				if (SetProperty(ref normalMinLength, value))
				{
					OnPropertyChanged(nameof(MinLength));
				}
			}
		}

		[LiteDB.BsonIgnore]
		public double MinLength => UserCollapsed ? 0 : NormalMinLength;

		[LiteDB.BsonIgnore]
		public Visibility Visibility => UserCollapsed ? Visibility.Collapsed : Visibility.Visible;

		private bool userCollapsed;

		public bool UserCollapsed
		{
			get => userCollapsed;
			set
			{
				if (SetProperty(ref userCollapsed, value))
					UpdateVisibility();
			}
		}

		[LiteDB.BsonIgnore]
		public GridLength Length
		{
			get => UserCollapsed ? new GridLength(0) : UserLength;
		}

		private const int gridSplitterWidth = 12;

		[LiteDB.BsonIgnore]
		public GridLength LengthIncludingGridSplitter
		{
			get => UserCollapsed ? new GridLength(0) : new GridLength(UserLength.Value + (IsResizeable ? gridSplitterWidth : 0));
		}

		[LiteDB.BsonIgnore]
		public bool IsResizeable { get; set; } = true;

		private GridLength userLength = new GridLength(200, GridUnitType.Pixel);

		[LiteDB.BsonIgnore]
		public GridLength UserLength
		{
			get => userLength;
			set
			{
				if (SetProperty(ref userLength, value))
				{
					OnPropertyChanged(nameof(Length));
					OnPropertyChanged(nameof(LengthIncludingGridSplitter));
				}
			}
		}

		public double UserLengthPixels
		{
			get => UserLength.Value;
			set => UserLength = new GridLength(value, GridUnitType.Pixel);
		}

		public void Hide()
		{
			UserCollapsed = true;
			IsHidden = true;
			UpdateVisibility();
		}

		public void Show()
		{
			UserCollapsed = false;
			IsHidden = false;
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			OnPropertyChanged(nameof(Length));
			OnPropertyChanged(nameof(LengthIncludingGridSplitter));
			OnPropertyChanged(nameof(MaxLength));
			OnPropertyChanged(nameof(Visibility));
			OnPropertyChanged(nameof(MinLength));
		}

		public void TryMultiplySize(double factor)
		{
			var newSize = Length.Value * factor;
			if (newSize == 0)
				return;

			double setLength = newSize;
			if (newSize < MinLength)
				setLength = MinLength;
			else if (newSize >= MaxLength)
				setLength = MaxLength;

			UserLength = new GridLength(setLength);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnViewModel model)
			{
				return (
					model.UserCollapsed == UserCollapsed &&
					model.Length.Value == Length.Value &&
					model.LengthIncludingGridSplitter.Value == LengthIncludingGridSplitter.Value &&
					model.UserLength.Value == UserLength.Value);
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = UserCollapsed.GetHashCode();
			hashCode = (hashCode * 397) ^ Length.Value.GetHashCode();
			hashCode = (hashCode * 397) ^ LengthIncludingGridSplitter.Value.GetHashCode();
			hashCode = (hashCode * 397) ^ UserLength.Value.GetHashCode();

			return hashCode;
		}
	}
}
