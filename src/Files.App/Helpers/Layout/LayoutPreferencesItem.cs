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

		public FolderLayoutModes LayoutMode { get; set; }

		public SortOption DirectorySortOption { get; set; }
		private string _DirectorySortPropertyId = string.Empty;
		public string DirectorySortPropertyId
		{
			get => _DirectorySortPropertyId;
			set => _DirectorySortPropertyId = value ?? string.Empty;
		}
		public SortDirection DirectorySortDirection { get; set; }
		public SortDirection DirectoryGroupDirection { get; set; }

		public GroupOption DirectoryGroupOption { get; set; }

		private string _DirectoryGroupPropertyId = string.Empty;
		public string DirectoryGroupPropertyId
		{
			get => _DirectoryGroupPropertyId;
			set => _DirectoryGroupPropertyId = value ?? string.Empty;
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
			DirectorySortPropertyId = template.SortPropertyId;
			DirectorySortDirection = template.SortDirection;
			SortDirectoriesAlongsideFiles = template.SortDirectoriesAlongsideFiles;
			SortFilesFirst = template.SortFilesFirst;
			DirectoryGroupOption = template.GroupOption;
			DirectoryGroupPropertyId = template.GroupPropertyId;
			DirectoryGroupDirection = template.GroupDirection;
			DirectoryGroupByDateUnit = template.GroupByDateUnit;

			ColumnsViewModel = new ColumnsViewModel();
			DetailsLayoutColumnDefaults.Apply(ColumnsViewModel, template);
		}

		// Methods

		public static LayoutPreferencesItem FromTemplate(LayoutTemplate template)
			=> new(template);

		/// <summary>
		/// Copies these preferences to the specified template.
		/// </summary>
		public void SaveToTemplate(LayoutTemplate template)
		{
			template.LayoutMode = LayoutMode;
			template.SortOption = DirectorySortOption;
			template.SortPropertyId = DirectorySortPropertyId;
			template.SortDirection = DirectorySortDirection;
			template.SortDirectoriesAlongsideFiles = SortDirectoriesAlongsideFiles;
			template.SortFilesFirst = SortFilesFirst;
			template.GroupOption = DirectoryGroupOption;
			template.GroupPropertyId = DirectoryGroupPropertyId;
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
					item.DirectoryGroupPropertyId == DirectoryGroupPropertyId &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortPropertyId == DirectorySortPropertyId &&
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
			hash.Add(DirectoryGroupPropertyId);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortPropertyId);
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
