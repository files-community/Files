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
	/// Provides the shell properties Windows allows to be shown as columns.
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
			var listId = typeof(IPropertyDescriptionList).GUID;
			if (PInvoke.PSEnumeratePropertyDescriptions(PROPDESC_ENUMFILTER.PDEF_VIEWABLE, &listId, out var listObject).Failed)
				return properties;

			if (listObject is not IPropertyDescriptionList list || list.GetCount(out uint count).Failed)
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
