// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public sealed class LayoutPreferencesItem
	{
		// Properties

		public ColumnsViewModel ColumnsViewModel { get; set; }

		public bool SortDirectoriesAlongsideFiles { get; set; }
		public bool SortFilesFirst { get; set; }

		/// <summary>
		/// Gets or sets the id of the layout template these preferences were created from.
		/// </summary>
		public string TemplateId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether the folder uses its template as is, instead of its own preferences.
		/// </summary>
		public bool FollowsTemplate { get; set; }

		public FolderLayoutModes LayoutMode { get; set; }

		public SortOption DirectorySortOption { get; set; }
		private string _DirectorySortColumnId = string.Empty;
		public string DirectorySortColumnId
		{
			get => _DirectorySortColumnId;
			set => _DirectorySortColumnId = value ?? string.Empty;
		}
		public SortDirection DirectorySortDirection { get; set; }
		public SortDirection DirectoryGroupDirection { get; set; }

		public GroupOption DirectoryGroupOption { get; set; }

		private string _DirectoryGroupColumnId = string.Empty;
		public string DirectoryGroupColumnId
		{
			get => _DirectoryGroupColumnId;
			set => _DirectoryGroupColumnId = value ?? string.Empty;
		}
		public GroupByDateUnit DirectoryGroupByDateUnit { get; set; }

		// Constructors

		public LayoutPreferencesItem()
			: this(Ioc.Default.GetRequiredService<ILayoutTemplatesService>().DefaultTemplate)
		{
		}

		private LayoutPreferencesItem(LayoutTemplate template)
		{
			TemplateId = template.Id;
			LayoutMode = template.LayoutMode;
			DirectorySortOption = template.SortOption;
			DirectorySortColumnId = template.SortColumnId;
			DirectorySortDirection = template.SortDirection;
			SortDirectoriesAlongsideFiles = template.SortDirectoriesAlongsideFiles;
			SortFilesFirst = template.SortFilesFirst;
			DirectoryGroupOption = template.GroupOption;
			DirectoryGroupColumnId = template.GroupColumnId;
			DirectoryGroupDirection = template.GroupDirection;
			DirectoryGroupByDateUnit = template.GroupByDateUnit;

			ColumnsViewModel = new ColumnsViewModel();
			DetailsLayoutColumnDefaults.Apply(ColumnsViewModel, template);
		}

		// Methods

		public static LayoutPreferencesItem FromTemplate(LayoutTemplate template)
			=> new(template) { FollowsTemplate = true };

		/// <summary>
		/// Copies these preferences to the specified template.
		/// </summary>
		public void SaveToTemplate(LayoutTemplate template)
		{
			template.LayoutMode = LayoutMode;
			template.SortOption = DirectorySortOption;
			template.SortColumnId = DirectorySortColumnId;
			template.SortDirection = DirectorySortDirection;
			template.SortDirectoriesAlongsideFiles = SortDirectoriesAlongsideFiles;
			template.SortFilesFirst = SortFilesFirst;
			template.GroupOption = DirectoryGroupOption;
			template.GroupColumnId = DirectoryGroupColumnId;
			template.GroupDirection = DirectoryGroupDirection;
			template.GroupByDateUnit = DirectoryGroupByDateUnit;

			DetailsLayoutColumnDefaults.Save(ColumnsViewModel, template);
		}

		// Overridden methods

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is LayoutPreferencesItem item)
			{
				return (
					item.LayoutMode == LayoutMode &&
					item.DirectoryGroupOption == DirectoryGroupOption &&
					item.DirectoryGroupColumnId == DirectoryGroupColumnId &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortColumnId == DirectorySortColumnId &&
					item.DirectorySortDirection == DirectorySortDirection &&
					item.DirectoryGroupDirection == DirectoryGroupDirection &&
					item.DirectoryGroupByDateUnit == DirectoryGroupByDateUnit &&
					item.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					item.SortFilesFirst == SortFilesFirst &&
					item.ColumnsViewModel.Equals(ColumnsViewModel));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			HashCode hash = new();

			hash.Add(LayoutMode);
			hash.Add(DirectoryGroupOption);
			hash.Add(DirectoryGroupColumnId);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortColumnId);
			hash.Add(DirectorySortDirection);
			hash.Add(DirectoryGroupDirection);
			hash.Add(DirectoryGroupByDateUnit);
			hash.Add(SortDirectoriesAlongsideFiles);
			hash.Add(SortFilesFirst);
			hash.Add(ColumnsViewModel);

			return hash.ToHashCode();
		}
	}
}
