// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for a column shown in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public class DetailsLayoutColumnItem : ObservableObject, IDetailsLayoutColumnItem
	{
		// Store to the database since user can change
		private double _Width = 200d;
		public double Width
		{
			get => IsVisible ? _Width : 0d;
			set
			{
				if (SetProperty(ref _Width, value))
					OnPropertyChanged(nameof(WidthWithGridSplitter));
			}
		}

		// Store to the database since user can change
		private bool _IsVisible = true;
		public bool IsVisible
		{
			get => _IsVisible && IsAvailable;
			set
			{
				if (SetProperty(ref _IsVisible, value))
					NotifyChanges();
			}
		}

		[LiteDB.BsonIgnore]
		public string Name { get; set; } = string.Empty;

		[LiteDB.BsonIgnore]
		public DetailsLayoutColumnKind Kind { get; set; }

		[LiteDB.BsonIgnore]
		public bool IsSortDisabled { get; set; }

		[LiteDB.BsonIgnore]
		public bool IsResizable { get; set; } = true;

		[LiteDB.BsonIgnore]
		public double WidthWithGridSplitter
			=> IsVisible ? Width + 12d : 0d;

		private SortDirection? _SortDirection;
		[LiteDB.BsonIgnore]
		public SortDirection? SortDirection
		{
			get => _SortDirection;
			set => SetProperty(ref _SortDirection, value);
		}

		private bool _IsAvailable = true;
		[LiteDB.BsonIgnore]
		public bool IsAvailable
		{
			get => _IsAvailable;
			set
			{
				if (SetProperty(ref _IsAvailable, value))
					NotifyChanges();
			}
		}

		private double _MinWidth = 50d;
		[LiteDB.BsonIgnore]
		public double MinWidth
		{
			get => IsVisible ? _MinWidth : 0d;
			set => SetProperty(ref _MinWidth, value);
		}

		private double _MaxWidth = 800d;
		[LiteDB.BsonIgnore]
		public double MaxWidth
		{
			get => IsVisible ? _MaxWidth : 0d;
			set => SetProperty(ref _MaxWidth, value);
		}

		public DetailsLayoutColumnItem()
		{
		}

		public DetailsLayoutColumnItem(DetailsLayoutColumnKind kind, double width, bool isAvailable)
		{
			Kind = kind;
			Width = width;
			IsAvailable = isAvailable;
		}

		public void NotifyChanges()
		{
			OnPropertyChanged(nameof(IsVisible));
			OnPropertyChanged(nameof(IsAvailable));
			OnPropertyChanged(nameof(Width));
			OnPropertyChanged(nameof(MaxWidth));
			OnPropertyChanged(nameof(MinWidth));
			OnPropertyChanged(nameof(WidthWithGridSplitter));
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is DetailsLayoutColumnItem model)
			{
				return
					model.IsVisible == IsVisible &&
					model.Width == Width;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(IsVisible, Width);
		}

		public static DetailsLayoutColumnItemModel ToModel(DetailsLayoutColumnItem item)
		{
			return new()
			{
				Kind = item.Kind,
				Width = item.Width,
				IsVisible = item.IsVisible,
			};
		}

		public static DetailsLayoutColumnItem ToItem(DetailsLayoutColumnItemModel model)
		{
			return new()
			{
				Kind = model.Kind,
				Width = model.Width,
				IsVisible = model.IsVisible,
			};
		}
	}
}
