// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	// TODO: Must be drive from IList
	public class DetailsLayoutColumnItemCollection : ObservableObject
	{
		private DetailsLayoutColumnItem _IconColumn = new() { UserLength = new(24), IsResizable = false };
		[LiteDB.BsonIgnore]
		public DetailsLayoutColumnItem IconColumn
		{
			get => _IconColumn;
			set => SetProperty(ref _IconColumn, value);
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

		private DetailsLayoutColumnItem nameColumn = new() { NormalMaxLength = 1000d };
		public DetailsLayoutColumnItem NameColumn
		{
			get => nameColumn;
			set => SetProperty(ref nameColumn, value);
		}

		private DetailsLayoutColumnItem statusColumn = new() { UserLength = new GridLength(50), NormalMaxLength = 80, };
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

		private DetailsLayoutColumnItem pathColumn = new() { NormalMaxLength = 500, };
		public DetailsLayoutColumnItem PathColumn
		{
			get => pathColumn;
			set => SetProperty(ref pathColumn, value);
		}

		private DetailsLayoutColumnItem originalPathColumn = new() { NormalMaxLength = 500, };
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

		private DetailsLayoutColumnItem dateCreatedColumn = new() { UserCollapsed = true };
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

		[LiteDB.BsonIgnore]
		public double TotalWidth =>
			IconColumn.Length.Value +
			GitStatusColumn.Length.Value +
			GitLastCommitDateColumn.Length.Value +
			GitLastCommitMessageColumn.Length.Value +
			GitCommitAuthorColumn.Length.Value +
			GitLastCommitShaColumn.Length.Value +
			TagColumn.Length.Value +
			NameColumn.Length.Value +
			DateModifiedColumn.Length.Value +
			PathColumn.Length.Value +
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
			GitStatusColumn.TryMultiplySize(factor);
			GitLastCommitDateColumn.TryMultiplySize(factor);
			GitLastCommitMessageColumn.TryMultiplySize(factor);
			GitCommitAuthorColumn.TryMultiplySize(factor);
			GitLastCommitShaColumn.TryMultiplySize(factor);
			TagColumn.TryMultiplySize(factor);
			DateModifiedColumn.TryMultiplySize(factor);
			PathColumn.TryMultiplySize(factor);
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

			if (obj is DetailsLayoutColumnItemCollection model)
			{
				return
					model.DateCreatedColumn.Equals(DateCreatedColumn) &&
					model.DateDeletedColumn.Equals(DateDeletedColumn) &&
					model.DateModifiedColumn.Equals(DateModifiedColumn) &&
					model.ItemTypeColumn.Equals(ItemTypeColumn) &&
					model.NameColumn.Equals(NameColumn) &&
					model.PathColumn.Equals(PathColumn) &&
					model.OriginalPathColumn.Equals(OriginalPathColumn) &&
					model.SizeColumn.Equals(SizeColumn) &&
					model.StatusColumn.Equals(StatusColumn) &&
					model.TagColumn.Equals(TagColumn) &&
					model.GitStatusColumn.Equals(GitStatusColumn) &&
					model.GitLastCommitDateColumn.Equals(GitLastCommitDateColumn) &&
					model.GitLastCommitMessageColumn.Equals(GitLastCommitMessageColumn) &&
					model.GitCommitAuthorColumn.Equals(GitCommitAuthorColumn) &&
					model.GitLastCommitShaColumn.Equals(GitLastCommitShaColumn);
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(DateCreatedColumn);
			hash.Add(DateDeletedColumn);
			hash.Add(DateModifiedColumn);
			hash.Add(ItemTypeColumn);
			hash.Add(NameColumn);
			hash.Add(PathColumn);
			hash.Add(OriginalPathColumn);
			hash.Add(SizeColumn);
			hash.Add(StatusColumn);
			hash.Add(TagColumn);
			hash.Add(GitStatusColumn);
			hash.Add(GitLastCommitDateColumn);
			hash.Add(GitLastCommitMessageColumn);
			hash.Add(GitCommitAuthorColumn);
			hash.Add(GitLastCommitShaColumn);

			return hash.ToHashCode();
		}
	}
}
