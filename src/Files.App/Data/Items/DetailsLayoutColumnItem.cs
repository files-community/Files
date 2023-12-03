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
		private const int GRID_SPLITTER_WIDTH = 12;

		private double _UserLengthPixels;
		public double UserLengthPixels
		{
			get => _UserLengthPixels;
			set => SetProperty(ref _UserLengthPixels, value);
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
		public string Name { get; set; } = string.Empty;

		[LiteDB.BsonIgnore]
		public DetailsLayoutColumnKind Kind { get; set; }

		[LiteDB.BsonIgnore]
		public bool IsSortDisabled { get; set; }

		[LiteDB.BsonIgnore]
		public SortDirection? SortDirection { get; set; }

		[LiteDB.BsonIgnore]
		public bool IsResizable { get; set; } = true;

		[LiteDB.BsonIgnore]
		public double MinLength
			=> UserCollapsed || IsHidden ? 0 : NormalMinLength;

		[LiteDB.BsonIgnore]
		public Visibility Visibility
			=> UserCollapsed || IsHidden ? Visibility.Collapsed : Visibility.Visible;

		[LiteDB.BsonIgnore]
		public GridLength Length
			=> new(UserCollapsed || IsHidden ? 0 : UserLengthPixels);

		[LiteDB.BsonIgnore]
		public GridLength LengthIncludingGridSplitter =>
			UserCollapsed || IsHidden
				? new(0)
				: new(UserLengthPixels + (IsResizable ? GRID_SPLITTER_WIDTH : 0));

		[LiteDB.BsonIgnore]
		public double MaxLength
			=> UserCollapsed || IsHidden ? 0 : NormalMaxLength;

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
					OnPropertyChanged(nameof(MinLength));
			}
		}

		public void Hide()
		{
			IsHidden = true;
			UpdateVisibility();
		}

		public void Show()
		{
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

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is DetailsLayoutColumnItem model)
			{
				return
					model.UserCollapsed == UserCollapsed &&
					model.Length.Value == Length.Value &&
					model.LengthIncludingGridSplitter.Value == LengthIncludingGridSplitter.Value &&
					model.UserLengthPixels == UserLengthPixels;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = UserCollapsed.GetHashCode();

			hashCode = (hashCode * 397) ^ Length.Value.GetHashCode();
			hashCode = (hashCode * 397) ^ LengthIncludingGridSplitter.Value.GetHashCode();
			hashCode = (hashCode * 397) ^ UserLengthPixels.GetHashCode();

			return hashCode;
		}
	}
}
