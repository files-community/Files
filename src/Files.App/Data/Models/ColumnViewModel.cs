// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public class ColumnViewModel : ObservableObject
	{
		private const int GRID_SPLITTER_WIDTH = 12;

		private bool isHidden;
		[LiteDB.BsonIgnore]
		public bool IsHidden
		{
			get => isHidden;
			set => SetProperty(ref isHidden, value);
		}

		private double normalMinLength = 50;
		[LiteDB.BsonIgnore]
		public double NormalMinLength
		{
			get => normalMinLength;
			set
			{
				if (SetProperty(ref normalMinLength, value))
					OnPropertyChanged(nameof(MinLength));
			}
		}

		private double normalMaxLength = 800;
		public double NormalMaxLength
		{
			get => normalMaxLength;
			set => SetProperty(ref normalMaxLength, value);
		}

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
		public double MaxLength
			=> UserCollapsed ? 0 : NormalMaxLength;

		[LiteDB.BsonIgnore]
		public double MinLength
			=> UserCollapsed ? 0 : NormalMinLength;

		[LiteDB.BsonIgnore]
		public Visibility Visibility
			=> UserCollapsed ? Visibility.Collapsed : Visibility.Visible;

		[LiteDB.BsonIgnore]
		public GridLength Length
			=> UserCollapsed ? new GridLength(0) : UserLength;

		[LiteDB.BsonIgnore]
		public GridLength LengthIncludingGridSplitter
			=> UserCollapsed ? new(0) : new(UserLength.Value + (IsResizable ? GRID_SPLITTER_WIDTH : 0));

		[LiteDB.BsonIgnore]
		public bool IsResizable { get; set; } = true;

		private GridLength userLength = new(200, GridUnitType.Pixel);
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
