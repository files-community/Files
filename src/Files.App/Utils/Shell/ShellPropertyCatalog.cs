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
	/// <param name="IsDate">Whether the values are dates, which can be grouped by year, month or day.</param>
	public sealed record ShellPropertyColumnInfo(string Name, string DisplayName, double DefaultWidth, bool IsDate);

	/// <summary>
	/// Provides the shell properties that can be shown as columns: the ones Windows allows, and the ones the properties window or the preview pane show.
	/// </summary>
	public static class ShellPropertyCatalog
	{
		private const double PixelsPerCharacter = 7;
		private const double MinColumnWidth = 60;
		private const double MaxColumnWidth = 400;

		private static Task<IReadOnlyDictionary<string, ShellPropertyColumnInfo>>? _loadTask;

		public static Task<IReadOnlyDictionary<string, ShellPropertyColumnInfo>> GetAsync()
		{
			return _loadTask ??= STATask.RunPooled(Enumerate, App.Logger);
		}

		private static unsafe IReadOnlyDictionary<string, ShellPropertyColumnInfo> Enumerate()
		{
			var properties = new Dictionary<string, ShellPropertyColumnInfo>();
			AddProperties(properties, PROPDESC_ENUMFILTER.PDEF_VIEWABLE, null);

			var shownPropertyIds = DetailsLayoutColumnGroups.ShownPropertyIds.Where(id => !properties.ContainsKey(id)).ToHashSet();
			if (shownPropertyIds.Count > 0)
				AddProperties(properties, PROPDESC_ENUMFILTER.PDEF_ALL, shownPropertyIds);

			return properties;
		}

		/// <param name="names">The properties to add, or null to add all that match the filter.</param>
		private static unsafe void AddProperties(Dictionary<string, ShellPropertyColumnInfo> properties, PROPDESC_ENUMFILTER filter, HashSet<string>? names)
		{
			var listId = typeof(IPropertyDescriptionList).GUID;
			if (PInvoke.PSEnumeratePropertyDescriptions(filter, &listId, out var listObject).Failed)
				return;

			if (listObject is not IPropertyDescriptionList list || list.GetCount(out uint count).Failed)
				return;

			var descriptionId = typeof(IPropertyDescription).GUID;
			for (uint i = 0; i < count; i++)
			{
				if (list.GetAt(i, &descriptionId, out void* descriptionPointer).Failed)
					continue;

				try
				{
					if (ComInterfaceMarshaller<IPropertyDescription>.ConvertToManaged(descriptionPointer) is { } description &&
						CreateInfo(description) is { } info &&
						names?.Contains(info.Name) is not false)
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

				var width = Math.Clamp(characters * PixelsPerCharacter + 24, MinColumnWidth, MaxColumnWidth);

				PROPDESC_DISPLAYTYPE displayType = default;
				description.GetDisplayType(&displayType);

				return new(nameText, displayNameText, width, displayType is PROPDESC_DISPLAYTYPE.PDDT_DATETIME);
			}
			finally
			{
				if (name.Value is not null)
					PInvoke.CoTaskMemFree(name);
				if (displayName.Value is not null)
					PInvoke.CoTaskMemFree(displayName);
			}
		}
	}
}
