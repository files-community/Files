// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for a column shown in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public class DetailsLayoutColumnItem : ObservableObject
	{
		private const int GRID_SPLITTER_WIDTH = 12;

		public double UserLengthPixels
		{
			get => UserLength.Value;
			set => UserLength = new GridLength(value, GridUnitType.Pixel);
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
		public bool IsResizable { get; set; } = true;

		[LiteDB.BsonIgnore]
		public double MinLength
			=> UserCollapsed || IsHidden ? 0 : NormalMinLength;

		[LiteDB.BsonIgnore]
		public Visibility Visibility
			=> UserCollapsed || IsHidden ? Visibility.Collapsed : Visibility.Visible;

		[LiteDB.BsonIgnore]
		public GridLength Length
			=> UserCollapsed || IsHidden ? new GridLength(0) : UserLength;

		[LiteDB.BsonIgnore]
		public GridLength LengthIncludingGridSplitter =>
			UserCollapsed || IsHidden
				? new(0)
				: new(UserLength.Value + (IsResizable ? GRID_SPLITTER_WIDTH : 0));

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

		private GridLength _UserLength = new(200, GridUnitType.Pixel);
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
