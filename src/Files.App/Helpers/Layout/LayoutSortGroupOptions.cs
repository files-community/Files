// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Utils.Shell;
using Files.Shared.Helpers;
using System.Collections.Frozen;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents an option of the sort or group menus.
	/// </summary>
	/// <param name="BuiltInCommand">The built-in sort or group command, when the option is one.</param>
	/// <param name="PropertyId">The Shell property or Git column the option uses, when it isn't built in.</param>
	/// <param name="Items">The options of a submenu, such as the date units of a date.</param>
	/// <param name="DateUnit">The date unit to group by, for the date units of a date property.</param>
	/// <param name="IsSeparator">Whether the option is a separator between groups of options.</param>
	internal sealed record LayoutSortGroupOption(
		string Label,
		IRichCommand? BuiltInCommand = null,
		string? PropertyId = null,
		bool IsChecked = false,
		IReadOnlyList<LayoutSortGroupOption>? Items = null,
		GroupByDateUnit? DateUnit = null,
		bool IsSeparator = false);

	/// <summary>
	/// Provides the sort and group options of a location, which depend on the type of its content.
	/// </summary>
	internal static class LayoutSortGroupOptions
	{
		private const string AuthorsId = "System.Author";
		private const string TitleId = "System.Title";
		private const string DateId = "System.ItemDate";
		private const string DateTakenId = "System.Photo.DateTaken";
		private const string DimensionsId = "System.Image.Dimensions";
		private const string RatingId = "System.Rating";
		private const string LengthId = "System.Media.Duration";
		private const string MediaCreatedId = "System.Media.DateEncoded";
		private const string TrackNumberId = "System.Music.TrackNumber";
		private const string ArtistId = "System.Music.Artist";
		private const string AlbumId = "System.Music.AlbumTitle";
		private const string AlbumArtistId = "System.Music.AlbumArtist";
		private const string BitRateId = "System.Audio.EncodingBitrate";
		private const string GenreId = "System.Music.Genre";
		private const string ProtectedId = "System.DRM.IsProtected";
		private const string YearId = "System.Media.Year";

		// File Explorer's Tags property, which the options leave out
		private const string ExplorerTagsId = "System.Keywords";

		private enum FolderType
		{
			General,
			Pictures,
			Videos,
			Music,
		}

		/// <param name="DateGroups">The date property grouped by day, month or year at the top of the group options, with the labels of the units.</param>
		private sealed record LocationOptions(string[] Sort, string[] Group, (string PropertyId, string Day, string Month, string Year)? DateGroups = null);

		private static readonly FrozenDictionary<FolderType, LocationOptions> _folderTypeOptions = new Dictionary<FolderType, LocationOptions>()
		{
			[FolderType.General] = new(
				[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.DateModified, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.DateCreated, AuthorsId, TitleId, DetailsLayoutColumnIds.Tag],
				[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, AuthorsId, TitleId, DetailsLayoutColumnIds.Tag]),
			[FolderType.Pictures] = new(
				[DetailsLayoutColumnIds.Name, DateId, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.DateCreated, DetailsLayoutColumnIds.DateModified, DateTakenId, DimensionsId, RatingId, DetailsLayoutColumnIds.Tag],
				[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, DimensionsId, RatingId, DetailsLayoutColumnIds.Tag],
				(DateTakenId, Strings.DayTaken, Strings.MonthTaken, Strings.YearTaken)),
			[FolderType.Videos] = new(
				[DetailsLayoutColumnIds.Name, DateId, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, LengthId, DetailsLayoutColumnIds.DateCreated, DetailsLayoutColumnIds.DateModified, MediaCreatedId, DimensionsId, DetailsLayoutColumnIds.Tag],
				[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, LengthId, DimensionsId, DetailsLayoutColumnIds.Tag],
				(MediaCreatedId, Strings.DayRecorded, Strings.MonthRecorded, Strings.YearRecorded)),
			[FolderType.Music] = new(
				[DetailsLayoutColumnIds.Name, TrackNumberId, TitleId, ArtistId, AlbumId, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.DateCreated, DetailsLayoutColumnIds.DateModified, AlbumArtistId, BitRateId, GenreId, LengthId, ProtectedId, RatingId, YearId, DetailsLayoutColumnIds.Tag],
				[DetailsLayoutColumnIds.Name, TrackNumberId, TitleId, ArtistId, AlbumId, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Size, AlbumArtistId, BitRateId, GenreId, LengthId, ProtectedId, RatingId, YearId, DetailsLayoutColumnIds.Tag]),
		}.ToFrozenDictionary();

		private static readonly LocationOptions RecycleBinOptions = new(
			[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.OriginalPath, DetailsLayoutColumnIds.DateDeleted, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.DateModified, DetailsLayoutColumnIds.Tag],
			[DetailsLayoutColumnIds.Name, DetailsLayoutColumnIds.OriginalPath, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.Tag]);

		/// <summary>
		/// Gets the Git columns that items can be sorted by.
		/// </summary>
		public static IReadOnlyList<string> GitSortColumnIds { get; } = DetailsLayoutColumnDefinitions.BuiltIn.Where(definition => definition.IsGitColumn).Select(definition => definition.Id).ToArray();

		/// <summary>
		/// Gets the Git columns that items can be grouped by.
		/// </summary>
		public static IReadOnlyList<string> GitGroupColumnIds { get; } = new[] { DetailsLayoutColumnIds.GitStatus, DetailsLayoutColumnIds.GitCommitAuthor, DetailsLayoutColumnIds.GitLastCommitDate };

		private static readonly FrozenSet<string> PictureExtensions = FileExtensionHelpers.PictureExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		private static readonly FrozenSet<string> VideoExtensions = FileExtensionHelpers.VideoExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		private static readonly FrozenSet<string> AudioExtensions = FileExtensionHelpers.AudioExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		private static readonly int ContentThreshold = new LayoutRule().Threshold;

		private static (WeakReference<ShellViewModel> Owner, string? Path, int Count, FolderType FolderType)? _lastFolderType;

		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		/// <summary>
		/// Gets the options of the sort menus for the specified location.
		/// </summary>
		public static IReadOnlyList<LayoutSortGroupOption> GetSortOptions(LayoutPreferencesManager folderSettings, ShellViewModel? shellViewModel)
		{
			var options = new List<LayoutSortGroupOption>();
			var commands = new HashSet<IRichCommand>();
			var sortPropertyId = folderSettings.DirectorySortOption is SortOption.Property ? folderSettings.DirectorySortPropertyId : null;

			foreach (var id in GetLocationOptions(shellViewModel).Sort.Append(DetailsLayoutColumnIds.Status).Append(DetailsLayoutColumnIds.Path))
				AddOption(id);

			if (shellViewModel?.IsValidGitDirectory is true)
			{
				foreach (var id in GitSortColumnIds)
					AddOption(id);
			}

			if (sortPropertyId is not null)
				AddOption(sortPropertyId);

			return options;

			void AddOption(string id)
			{
				if (GetBuiltInSortCommand(id) is { } command)
				{
					if (command.IsExecutable && commands.Add(command))
						options.Add(new(command.Label, command, IsChecked: command.IsOn));
				}
				else if (options.All(option => option.PropertyId != id))
				{
					options.Add(CreatePropertyOption(id, id == sortPropertyId));
				}
			}
		}

		/// <summary>
		/// Gets the options of the group menus for the specified location.
		/// </summary>
		public static IReadOnlyList<LayoutSortGroupOption> GetGroupOptions(LayoutPreferencesManager folderSettings, ShellViewModel? shellViewModel)
		{
			var options = new List<LayoutSortGroupOption>();
			var commands = new HashSet<IRichCommand>();
			var groupPropertyId = folderSettings.DirectoryGroupOption is GroupOption.Property ? folderSettings.DirectoryGroupPropertyId : null;
			var locationOptions = GetLocationOptions(shellViewModel);

			if (ReferenceEquals(locationOptions, RecycleBinOptions))
			{
				AddLabeledCommand(Commands.GroupByDateDeletedDay, Strings.DayDeleted);
				AddLabeledCommand(Commands.GroupByDateDeletedMonth, Strings.MonthDeleted);
				AddLabeledCommand(Commands.GroupByDateDeletedYear, Strings.YearDeleted);
			}
			else
			{
				AddLabeledCommand(Commands.GroupByDateModifiedDay, Strings.DayUpdated);
				AddLabeledCommand(Commands.GroupByDateModifiedMonth, Strings.MonthUpdated);
				AddLabeledCommand(Commands.GroupByDateModifiedYear, Strings.YearUpdated);
			}

			if (locationOptions.DateGroups is var (dateId, day, month, year))
			{
				AddDateUnitOption(dateId, GroupByDateUnit.Day, day);
				AddDateUnitOption(dateId, GroupByDateUnit.Month, month);
				AddDateUnitOption(dateId, GroupByDateUnit.Year, year);
			}

			var isGitRepository = shellViewModel?.IsValidGitDirectory is true;
			if (isGitRepository)
			{
				AddDateUnitOption(DetailsLayoutColumnIds.GitLastCommitDate, GroupByDateUnit.Day, Strings.DayCommitted);
				AddDateUnitOption(DetailsLayoutColumnIds.GitLastCommitDate, GroupByDateUnit.Month, Strings.MonthCommitted);
				AddDateUnitOption(DetailsLayoutColumnIds.GitLastCommitDate, GroupByDateUnit.Year, Strings.YearCommitted);
			}

			if (options.Count > 0)
				options.Add(new(string.Empty, IsSeparator: true));

			foreach (var id in locationOptions.Group.Append(DetailsLayoutColumnIds.Status).Append(DetailsLayoutColumnIds.Path))
				AddOption(id);

			if (isGitRepository)
			{
				foreach (var id in GitGroupColumnIds.Where(id => id != DetailsLayoutColumnIds.GitLastCommitDate))
					AddOption(id);
			}

			if (groupPropertyId is not null && options.All(option => option.PropertyId != groupPropertyId))
				options.Add(CreateGroupPropertyOption(groupPropertyId));

			if (Commands.GroupByNone.IsExecutable)
				options.Add(new(Strings.NoneInParentheses.GetLocalizedResource(), Commands.GroupByNone, IsChecked: Commands.GroupByNone.IsOn));

			return options;

			void AddOption(string id)
			{
				if (GetBuiltInGroupCommand(id) is { } command)
				{
					if (command.IsExecutable && commands.Add(command))
						options.Add(new(command.Label, command, IsChecked: command.IsOn));
				}
				else if (options.All(option => option.PropertyId != id))
				{
					options.Add(CreateGroupPropertyOption(id));
				}
			}

			void AddLabeledCommand(IRichCommand command, string label)
			{
				if (command.IsExecutable && commands.Add(command))
					options.Add(new(label.GetLocalizedResource(), command, IsChecked: command.IsOn));
			}

			void AddDateUnitOption(string id, GroupByDateUnit unit, string label)
			{
				var isChecked = id == groupPropertyId && folderSettings.DirectoryGroupByDateUnit == unit;
				options.Add(new(label.GetLocalizedResource(), PropertyId: id, IsChecked: isChecked, DateUnit: unit));
			}

			LayoutSortGroupOption CreateGroupPropertyOption(string id)
			{
				var option = CreatePropertyOption(id, id == groupPropertyId);
				if (!IsDateProperty(id))
					return option;

				return option with
				{
					Items = new[] { (Unit: GroupByDateUnit.Year, Label: Strings.Year), (Unit: GroupByDateUnit.Month, Label: Strings.Month), (Unit: GroupByDateUnit.Day, Label: Strings.Day) }
						.Select(date => new LayoutSortGroupOption(date.Label.GetLocalizedResource(), PropertyId: id, IsChecked: option.IsChecked && folderSettings.DirectoryGroupByDateUnit == date.Unit, DateUnit: date.Unit))
						.ToArray(),
				};
			}
		}

		/// <summary>
		/// Sorts the items of the folder by the specified property.
		/// </summary>
		public static void SortBy(LayoutPreferencesManager folderSettings, string id)
		{
			folderSettings.SetSortProperty(id);
			LayoutHelpers.UpdateOpenTabsPreferences();
		}

		/// <summary>
		/// Groups the items of the folder by the specified property.
		/// </summary>
		public static void GroupBy(LayoutPreferencesManager folderSettings, string id, GroupByDateUnit? dateUnit = null)
		{
			if (dateUnit is { } unit)
				folderSettings.DirectoryGroupByDateUnit = unit;

			folderSettings.SetGroupProperty(id);
			LayoutHelpers.UpdateOpenTabsPreferences();
		}

		/// <summary>
		/// Gets the columns that suit the location, such as the music columns of a music folder.
		/// </summary>
		public static IEnumerable<string> GetLocationColumnIds(ShellViewModel? shellViewModel)
			=> GetLocationOptions(shellViewModel).Sort;

		/// <summary>
		/// Gets the columns that suit any type of folder.
		/// </summary>
		public static IEnumerable<string> GetFolderTypeColumnIds()
			=> _folderTypeOptions.Values.SelectMany(options => options.Sort).Distinct();

		/// <summary>
		/// Gets whether the specified column is offered as a property option, instead of a built-in option such as path.
		/// </summary>
		public static bool IsPropertyOption(string id)
			=> GetBuiltInSortCommand(id) is null && id != ExplorerTagsId;

		/// <summary>
		/// Gets whether the values of the specified property are dates, which can be grouped by year, month or day.
		/// </summary>
		public static bool IsDateProperty(string id)
		{
			if (id == DetailsLayoutColumnIds.GitLastCommitDate)
				return true;

			return ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } task && task.Result.TryGetValue(id, out var info) && info.IsDate;
		}

		private static LocationOptions GetLocationOptions(ShellViewModel? shellViewModel)
		{
			if (shellViewModel?.WorkingDirectory is not { Length: > 0 } path)
				return _folderTypeOptions[FolderType.General];

			if (path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				return RecycleBinOptions;

			// Libraries and other shell locations offer their own columns
			if (!ShellFolderColumns.IsFileSystemPath(path) &&
				ShellFolderColumns.GetAsync(path) is { IsCompletedSuccessfully: true, Result.Count: > 0 } task)
			{
				var columns = task.Result.Where(column => column.IsPrimary && column.Id != ExplorerTagsId).ToArray();
				var dateGroups = columns.Any(column => column.Id == DateTakenId) ? _folderTypeOptions[FolderType.Pictures].DateGroups
					: columns.Any(column => column.Id == MediaCreatedId) ? _folderTypeOptions[FolderType.Videos].DateGroups
					: null;

				// Dates are grouped by the day, month and year options at the top instead
				return new(
					[.. columns.Select(column => column.Id), DetailsLayoutColumnIds.Tag],
					[.. columns.Where(column => column.CanGroup && !IsDateProperty(column.Id) && column.Id is not (DetailsLayoutColumnIds.DateModified or DetailsLayoutColumnIds.DateCreated)).Select(column => column.Id), DetailsLayoutColumnIds.Tag],
					dateGroups);
			}

			return _folderTypeOptions[GetFolderType(shellViewModel)];
		}

		private static FolderType GetFolderType(ShellViewModel shellViewModel)
		{
			var items = shellViewModel.FilesAndFolders;
			if (_lastFolderType is { } last && last.Owner.TryGetTarget(out var owner) && owner == shellViewModel && last.Path == shellViewModel.WorkingDirectory && last.Count == items.Count)
				return last.FolderType;

			var folderType = GetFolderType(FolderContentHelpers.GetContent(items));
			_lastFolderType = (new(shellViewModel), shellViewModel.WorkingDirectory, items.Count, folderType);
			return folderType;
		}

		private static FolderType GetFolderType(LayoutFolderContent content)
		{
			if (content.Total == 0)
				return FolderType.General;

			int images = 0, videos = 0, audio = 0;
			foreach (var (extension, count) in content.ExtensionCounts)
			{
				if (PictureExtensions.Contains(extension))
					images += count;
				else if (VideoExtensions.Contains(extension))
					videos += count;
				else if (AudioExtensions.Contains(extension))
					audio += count;
			}

			var threshold = content.Total * ContentThreshold / 100.0;
			if (images + videos >= threshold)
				return images >= videos ? FolderType.Pictures : FolderType.Videos;

			return audio >= threshold ? FolderType.Music : FolderType.General;
		}

		private static LayoutSortGroupOption CreatePropertyOption(string id, bool isChecked)
		{
			var catalog = ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } task ? task.Result : null;
			return new(DetailsLayoutColumnDefaults.GetDisplayName(id, catalog), PropertyId: id, IsChecked: isChecked);
		}

		private static IRichCommand? GetBuiltInSortCommand(string id)
		{
			return id switch
			{
				DetailsLayoutColumnIds.Name => Commands.SortByName,
				DetailsLayoutColumnIds.DateModified => Commands.SortByDateModified,
				DetailsLayoutColumnIds.DateCreated => Commands.SortByDateCreated,
				DetailsLayoutColumnIds.Size => Commands.SortBySize,
				DetailsLayoutColumnIds.ItemType => Commands.SortByType,
				DetailsLayoutColumnIds.Tag => Commands.SortByTag,
				DetailsLayoutColumnIds.Status => Commands.SortBySyncStatus,
				DetailsLayoutColumnIds.Path or "System.ItemFolderPathDisplay" => Commands.SortByPath,
				DetailsLayoutColumnIds.OriginalPath => Commands.SortByOriginalFolder,
				DetailsLayoutColumnIds.DateDeleted => Commands.SortByDateDeleted,
				_ => null,
			};
		}

		private static IRichCommand? GetBuiltInGroupCommand(string id)
		{
			return id switch
			{
				DetailsLayoutColumnIds.Name => Commands.GroupByName,
				DetailsLayoutColumnIds.Size => Commands.GroupBySize,
				DetailsLayoutColumnIds.ItemType => Commands.GroupByType,
				DetailsLayoutColumnIds.Tag => Commands.GroupByTag,
				DetailsLayoutColumnIds.Status => Commands.GroupBySyncStatus,
				DetailsLayoutColumnIds.Path or "System.ItemFolderPathDisplay" => Commands.GroupByFolderPath,
				DetailsLayoutColumnIds.OriginalPath => Commands.GroupByOriginalFolder,
				_ => null,
			};
		}
	}
}
