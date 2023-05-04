// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public class ColumnsViewModel : ObservableObject
	{
		private ColumnViewModel _IconColumn = new() { UserLength = new(24), IsResizeable = false };

		[LiteDB.BsonIgnore]
		public ColumnViewModel IconColumn
		{
			get => _IconColumn;
			set => SetProperty(ref _IconColumn, value);
		}

		private ColumnViewModel _TagColumn = new();
		public ColumnViewModel TagColumn
		{
			get => _TagColumn;
			set => SetProperty(ref _TagColumn, value);
		}

		private ColumnViewModel _NameColumn = new() { NormalMaxLength = 1000d };
		public ColumnViewModel NameColumn
		{
			get => _NameColumn;
			set => SetProperty(ref _NameColumn, value);
		}

		private ColumnViewModel _StatusColumn = new() { UserLength = new(50), NormalMaxLength = 80 };
		public ColumnViewModel StatusColumn
		{
			get => _StatusColumn;
			set => SetProperty(ref _StatusColumn, value);
		}

		private ColumnViewModel _DateModifiedColumn = new();
		public ColumnViewModel DateModifiedColumn
		{
			get => _DateModifiedColumn;
			set => SetProperty(ref _DateModifiedColumn, value);
		}

		private ColumnViewModel _OriginalPathColumn = new() { NormalMaxLength = 500 };
		public ColumnViewModel OriginalPathColumn
		{
			get => _OriginalPathColumn;
			set => SetProperty(ref _OriginalPathColumn, value);
		}

		private ColumnViewModel _ItemTypeColumn = new();
		public ColumnViewModel ItemTypeColumn
		{
			get => _ItemTypeColumn;
			set => SetProperty(ref _ItemTypeColumn, value);
		}

		private ColumnViewModel _DateDeletedColumn = new();
		public ColumnViewModel DateDeletedColumn
		{
			get => _DateDeletedColumn;
			set => SetProperty(ref _DateDeletedColumn, value);
		}

		private ColumnViewModel _DateCreatedColumn = new() { UserCollapsed = true };
		public ColumnViewModel DateCreatedColumn
		{
			get => _DateCreatedColumn;
			set => SetProperty(ref _DateCreatedColumn, value);
		}

		private ColumnViewModel _SizeColumn = new();
		public ColumnViewModel SizeColumn
		{
			get => _SizeColumn;
			set => SetProperty(ref _SizeColumn, value);
		}

		[LiteDB.BsonIgnore]
		public double TotalWidth =>
			IconColumn.Length.Value +
			TagColumn.Length.Value +
			NameColumn.Length.Value +
			DateModifiedColumn.Length.Value +
			OriginalPathColumn.Length.Value +
			ItemTypeColumn.Length.Value +
			DateDeletedColumn.Length.Value +
			DateCreatedColumn.Length.Value +
			SizeColumn.Length.Value +
			StatusColumn.Length.Value;

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
				return
					model.DateCreatedColumn.Equals(DateCreatedColumn) &&
					model.DateDeletedColumn.Equals(DateDeletedColumn) &&
					model.DateModifiedColumn.Equals(DateModifiedColumn) &&
					model.ItemTypeColumn.Equals(ItemTypeColumn) &&
					model.NameColumn.Equals(NameColumn) &&
					model.OriginalPathColumn.Equals(OriginalPathColumn) &&
					model.SizeColumn.Equals(SizeColumn) &&
					model.StatusColumn.Equals(StatusColumn) &&
					model.TagColumn.Equals(TagColumn);
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
		private bool _IsHidden;

		[LiteDB.BsonIgnore]
		public bool IsHidden
		{
			get => _IsHidden;
			set => SetProperty(ref _IsHidden, value);
		}

		private double _NormalMinLength = 50;

		[LiteDB.BsonIgnore]
		public double NormalMinLength
		{
			get => _NormalMinLength;
			set
			{
				if (SetProperty(ref _NormalMinLength, value))
				{
					OnPropertyChanged(nameof(MinLength));
				}
			}
		}

		private GridLength _UserLength = new(200);

		[LiteDB.BsonIgnore]
		public GridLength UserLength
		{
			get => _UserLength;
			set
			{
				if (SetProperty(ref _UserLength, value))
				{
					OnPropertyChanged(nameof(Length));
					OnPropertyChanged(nameof(LengthIncludingGridSplitter));
				}
			}
		}

		private double _NormalMaxLength = 800;
		public double NormalMaxLength
		{
			get => _NormalMaxLength;
			set => SetProperty(ref _NormalMaxLength, value);
		}

		private bool _UserCollapsed;
		public bool UserCollapsed
		{
			get => _UserCollapsed;
			set
			{
				if (SetProperty(ref _UserCollapsed, value))
					UpdateVisibility();
			}
		}

		[LiteDB.BsonIgnore]
		public double MaxLength
		{
			get => UserCollapsed ? 0 : NormalMaxLength;
		}

		[LiteDB.BsonIgnore]
		public double MinLength
			=> UserCollapsed ? 0 : NormalMinLength;

		[LiteDB.BsonIgnore]
		public Visibility Visibility
			=> UserCollapsed ? Visibility.Collapsed : Visibility.Visible;

		[LiteDB.BsonIgnore]
		public GridLength Length
		{
			get => UserCollapsed ? new GridLength(0) : UserLength;
		}

		private const int gridSplitterWidth = 12;

		[LiteDB.BsonIgnore]
		public GridLength LengthIncludingGridSplitter
			=> UserCollapsed ? new(0) : new(UserLength.Value + (IsResizeable ? gridSplitterWidth : 0));

		[LiteDB.BsonIgnore]
		public bool IsResizeable { get; set; } = true;

		public double UserLengthPixels
		{
			get => UserLength.Value;
			set => UserLength = new(value);
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

			UserLength = new(setLength);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnViewModel model)
			{
				return
					model.UserCollapsed == UserCollapsed &&
					model.Length.Value == Length.Value &&
					model.LengthIncludingGridSplitter.Value == LengthIncludingGridSplitter.Value &&
					model.UserLength.Value == UserLength.Value;
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
