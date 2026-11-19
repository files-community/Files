// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Helpers
{
	/// <summary>
	/// Sorts the columns of <see cref="DetailsLayoutPage"/> into the groups they are chosen from.
	/// </summary>
	internal static class DetailsLayoutColumnGroups
	{
		public enum ColumnGroup
		{
			General,
			Dates,
			Location,
			Description,
			Origin,
			Sharing,
			Documents,
			Pictures,
			Camera,
			Music,
			Videos,
			Media,
			RecordedTV,
			Email,
			Calendar,
			Tasks,
			Contacts,
			PhoneAndEmail,
			Addresses,
			Business,
			Devices,
			Git,
			Other,
		}

		private static readonly Dictionary<string, ColumnGroup> SystemColumnGroups = new()
		{
			["DateAccessed"] = ColumnGroup.Dates,
			["DateAcquired"] = ColumnGroup.Dates,
			["DateArchived"] = ColumnGroup.Dates,
			["DateCompleted"] = ColumnGroup.Dates,
			["DueDate"] = ColumnGroup.Dates,
			["StartDate"] = ColumnGroup.Dates,
			["EndDate"] = ColumnGroup.Dates,
			["ItemDate"] = ColumnGroup.Dates,
			["ItemFolderNameDisplay"] = ColumnGroup.Location,
			["ItemFolderPathDisplay"] = ColumnGroup.Location,
			["ItemFolderPathDisplayNarrow"] = ColumnGroup.Location,
			["ItemFolderPathDisplayNarrowNamespaceRelative"] = ColumnGroup.Location,
			["ItemSearchLocation"] = ColumnGroup.Location,
			["ComputerName"] = ColumnGroup.Location,
			["Title"] = ColumnGroup.Description,
			["Subject"] = ColumnGroup.Description,
			["Rating"] = ColumnGroup.Description,
			["Keywords"] = ColumnGroup.Description,
			["Category"] = ColumnGroup.Description,
			["Comment"] = ColumnGroup.Description,
			["Language"] = ColumnGroup.Description,
			["ParentalRating"] = ColumnGroup.Description,
			["ParentalRatingReason"] = ColumnGroup.Description,
			["TitleSortOverride"] = ColumnGroup.Description,
			["ContentStatus"] = ColumnGroup.Description,
			["Author"] = ColumnGroup.Origin,
			["ItemAuthors"] = ColumnGroup.Origin,
			["Company"] = ColumnGroup.Origin,
			["Copyright"] = ColumnGroup.Origin,
			["Trademarks"] = ColumnGroup.Origin,
			["Project"] = ColumnGroup.Origin,
			["ApplicationName"] = ColumnGroup.Origin,
			["FileDescription"] = ColumnGroup.Origin,
			["FileVersion"] = ColumnGroup.Origin,
			["SourceItem"] = ColumnGroup.Origin,
			["OfflineAvailability"] = ColumnGroup.Sharing,
			["OfflineStatus"] = ColumnGroup.Sharing,
			["IsShared"] = ColumnGroup.Sharing,
			["SharedWith"] = ColumnGroup.Sharing,
			["SharingStatus"] = ColumnGroup.Sharing,
			["HomeGroupSharingStatus"] = ColumnGroup.Sharing,
			["StorageProviderSharingStatus"] = ColumnGroup.Sharing,
			["StorageProviderState"] = ColumnGroup.Sharing,
			["IsAttachment"] = ColumnGroup.Email,
			["IsRead"] = ColumnGroup.Email,
			["ItemParticipants"] = ColumnGroup.Email,
			["FlagColor"] = ColumnGroup.Tasks,
			["FlagStatus"] = ColumnGroup.Tasks,
			["IsFlagged"] = ColumnGroup.Tasks,
			["IsFlaggedComplete"] = ColumnGroup.Tasks,
			["IsIncomplete"] = ColumnGroup.Tasks,
			["Priority"] = ColumnGroup.Tasks,
			["Importance"] = ColumnGroup.Tasks,
			["Sensitivity"] = ColumnGroup.Tasks,
			["Capacity"] = ColumnGroup.Devices,
			["FreeSpace"] = ColumnGroup.Devices,
			["PercentFull"] = ColumnGroup.Devices,
			["IsDeleted"] = ColumnGroup.Other,
			["FileCount"] = ColumnGroup.Other,
			["MileageInformation"] = ColumnGroup.Other,
		};

		private static readonly HashSet<string> PictureColumns = ["DateTaken", "Event", "PeopleNames", "Orientation"];

		private static readonly HashSet<string> BusinessContactColumns = ["AssistantName", "AssistantTelephone", "CompanyMainTelephone", "Department", "JobTitle", "OfficeLocation", "Profession"];

		/// <summary>
		/// Gets the group of the specified column.
		/// </summary>
		public static ColumnGroup GetGroup(string id)
		{
			if (DetailsLayoutColumnDefinitions.Get(id) is { } definition)
				return definition.IsGitColumn ? ColumnGroup.Git : ColumnGroup.General;

			var parts = id.Split('.');
			if (parts is not ["System", ..])
				return ColumnGroup.Other;

			if (parts.Length == 2)
				return SystemColumnGroups.GetValueOrDefault(parts[1], ColumnGroup.General);

			return parts[1] switch
			{
				"Document" or "Visio" or "Presentation" => ColumnGroup.Documents,
				"Image" => ColumnGroup.Pictures,
				"GPS" => ColumnGroup.Location,
				"Photo" => PictureColumns.Contains(parts[2]) ? ColumnGroup.Pictures : ColumnGroup.Camera,
				"Music" or "Audio" => ColumnGroup.Music,
				"Video" => ColumnGroup.Videos,
				"Media" or "DRM" => ColumnGroup.Media,
				"RecordedTV" => ColumnGroup.RecordedTV,
				"Message" => ColumnGroup.Email,
				"Communication" => parts[2] is "TaskStatus" ? ColumnGroup.Tasks : ColumnGroup.Email,
				"Calendar" or "Journal" => ColumnGroup.Calendar,
				"Task" => ColumnGroup.Tasks,
				"Contact" => GetContactGroup(parts[2]),
				"Software" => ColumnGroup.Origin,
				"Sharing" or "Sync" or "Home" => ColumnGroup.Sharing,
				"Devices" or "Volume" or "Computer" => ColumnGroup.Devices,
				"VersionControl" => ColumnGroup.Git,
				_ => ColumnGroup.Other,
			};
		}

		private static ColumnGroup GetContactGroup(string name)
		{
			if (name.StartsWith("Business", StringComparison.Ordinal) || BusinessContactColumns.Contains(name))
				return ColumnGroup.Business;

			if (name.Contains("Email", StringComparison.Ordinal) || name.Contains("Telephone", StringComparison.Ordinal) ||
				name.EndsWith("Number", StringComparison.Ordinal) || name is "IMAddress" or "WebPage")
				return ColumnGroup.PhoneAndEmail;

			return name.Contains("Address", StringComparison.Ordinal) ? ColumnGroup.Addresses : ColumnGroup.Contacts;
		}

		public static string GetDisplayName(ColumnGroup group)
		{
			return (group switch
			{
				ColumnGroup.General => Strings.General,
				ColumnGroup.Dates => Strings.Dates,
				ColumnGroup.Location => Strings.LocationColumns,
				ColumnGroup.Description => Strings.Description,
				ColumnGroup.Origin => Strings.Origin,
				ColumnGroup.Sharing => Strings.Sharing,
				ColumnGroup.Documents => Strings.Documents,
				ColumnGroup.Pictures => Strings.Pictures,
				ColumnGroup.Camera => Strings.Camera,
				ColumnGroup.Music => Strings.Music,
				ColumnGroup.Videos => Strings.Videos,
				ColumnGroup.Media => Strings.Media,
				ColumnGroup.RecordedTV => Strings.RecordedTV,
				ColumnGroup.Email => Strings.Email,
				ColumnGroup.Calendar => Strings.Calendar,
				ColumnGroup.Tasks => Strings.Tasks,
				ColumnGroup.Contacts => Strings.Contacts,
				ColumnGroup.PhoneAndEmail => Strings.PhoneAndEmail,
				ColumnGroup.Addresses => Strings.Addresses,
				ColumnGroup.Business => Strings.Business,
				ColumnGroup.Devices => Strings.DevicesAndDrives,
				ColumnGroup.Git => Strings.Git,
				_ => Strings.OtherColumns,
			}).GetLocalizedResource();
		}
	}
}
