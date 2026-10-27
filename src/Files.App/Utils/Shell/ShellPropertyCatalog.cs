// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents a shell property that can be shown as a column in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public sealed record ShellPropertyColumnInfo(string Name, string DisplayName, string Group, double DefaultWidth, PROPDESC_DISPLAYTYPE DisplayType);

	/// <summary>
	/// Provides the shell properties Windows allows to be shown as columns.
	/// </summary>
	public static class ShellPropertyCatalog
	{
		private const double PixelsPerCharacter = 7;
		private const double MinColumnWidth = 60;
		private const double MaxColumnWidth = 400;

		/// <summary>
		/// Gets the commonly used properties shown directly in the column menu, by category resource key.
		/// </summary>
		public static IReadOnlyList<(string ResourceKey, IReadOnlyList<string> PropertyNames)> MenuCategories { get; } =
		[
			(Strings.General, ["System.FileOwner", "System.FileAttributes", "System.DateAccessed", "System.Rating"]),
			(Strings.Music, ["System.Music.Artist", "System.Music.AlbumArtist", "System.Music.AlbumTitle", "System.Music.Genre", "System.Media.Year", "System.Music.TrackNumber", "System.Media.Duration", "System.Audio.EncodingBitrate"]),
			(Strings.Pictures, ["System.Photo.DateTaken", "System.Image.Dimensions", "System.Photo.CameraManufacturer", "System.Photo.CameraModel", "System.Photo.ExposureTime", "System.Photo.FNumber", "System.Photo.ISOSpeed"]),
			(Strings.Videos, ["System.Media.Duration", "System.Video.FrameWidth", "System.Video.FrameHeight", "System.Video.FrameRate", "System.Video.TotalBitrate"]),
			(Strings.Documents, ["System.Author", "System.Title", "System.Company", "System.Document.PageCount", "System.Document.WordCount", "System.Document.DateSaved", "System.Comment"]),
		];

		private static Task<IReadOnlyDictionary<string, ShellPropertyColumnInfo>>? _loadTask;

		public static Task<IReadOnlyDictionary<string, ShellPropertyColumnInfo>> GetAsync()
		{
			return _loadTask ??= STATask.RunPooled(Enumerate, App.Logger);
		}

		private static unsafe IReadOnlyDictionary<string, ShellPropertyColumnInfo> Enumerate()
		{
			var properties = new Dictionary<string, ShellPropertyColumnInfo>();
			if (PInvoke.PSEnumeratePropertyDescriptions(PROPDESC_ENUMFILTER.PDEF_VIEWABLE, typeof(IPropertyDescriptionList).GUID, out void* listPointer).Failed)
				return properties;

			try
			{
				var list = ComInterfaceMarshaller<IPropertyDescriptionList>.ConvertToManaged(listPointer);
				if (list is null || list.GetCount(out uint count).Failed)
					return properties;

				var descriptionId = typeof(IPropertyDescription).GUID;
				for (uint i = 0; i < count; i++)
				{
					if (list.GetAt(i, &descriptionId, out void* descriptionPointer).Failed)
						continue;

					try
					{
						if (ComInterfaceMarshaller<IPropertyDescription>.ConvertToManaged(descriptionPointer) is { } description &&
							CreateInfo(description) is { } info)
						{
							properties.TryAdd(info.Name, info);
						}
					}
					finally
					{
						ComInterfaceMarshaller<IPropertyDescription>.Free(descriptionPointer);
					}
				}
			}
			finally
			{
				ComInterfaceMarshaller<IPropertyDescriptionList>.Free(listPointer);
			}

			return properties;
		}

		private static unsafe ShellPropertyColumnInfo? CreateInfo(IPropertyDescription description)
		{
			PWSTR name = default;
			PWSTR displayName = default;
			try
			{
				if (description.GetCanonicalName(&name).Failed || description.GetDisplayName(&displayName).Failed)
					return null;

				var nameText = name.ToString();
				var displayNameText = displayName.ToString();
				if (string.IsNullOrEmpty(nameText) || string.IsNullOrEmpty(displayNameText))
					return null;

				description.GetDefaultColumnWidth(out uint characters);

				PROPDESC_DISPLAYTYPE displayType = default;
				description.GetDisplayType(&displayType);

				var width = Math.Clamp(characters * PixelsPerCharacter + 24, MinColumnWidth, MaxColumnWidth);

				return new(nameText, displayNameText, GetGroup(nameText), width, displayType);
			}
			finally
			{
				if (name.Value is not null)
					PInvoke.CoTaskMemFree(name);
				if (displayName.Value is not null)
					PInvoke.CoTaskMemFree(displayName);
			}
		}

		public static string GetGroupDisplayName(string group)
		{
			if (string.IsNullOrEmpty(group))
				return Strings.General.GetLocalizedResource();

			var displayName = $"PropertyGroup{group}".GetLocalizedResource();
			return string.IsNullOrEmpty(displayName) ? group : displayName;
		}

		private static string GetGroup(string name)
		{
			var parts = name.Split('.');
			return parts.Length > 2 ? parts[1] : string.Empty;
		}
	}
}
