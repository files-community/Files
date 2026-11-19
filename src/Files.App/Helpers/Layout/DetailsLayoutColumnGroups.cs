// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.ApplicationModel;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the groups that properties are chosen from, which the property list shared with the properties window and the preview pane defines.
	/// </summary>
	internal static class DetailsLayoutColumnGroups
	{
		private static readonly Lazy<(Dictionary<string, string> Groups, string[] GroupOrder, string[] ShownPropertyIds)> PropertyList = new(LoadPropertyList);

		/// <summary>
		/// Gets the properties the properties window or the preview pane show, which are offered as columns too.
		/// </summary>
		public static IReadOnlyCollection<string> ShownPropertyIds
			=> PropertyList.Value.ShownPropertyIds;

		/// <summary>
		/// Gets the group of the specified property, which is the last group for a property the list doesn't have.
		/// </summary>
		public static string GetGroup(string id)
			=> PropertyList.Value.Groups.GetValueOrDefault(id, Strings.Other);

		/// <summary>
		/// Gets the position of the specified group among the groups, which is the order the list has them in.
		/// </summary>
		public static int GetOrder(string group)
		{
			var index = Array.IndexOf(PropertyList.Value.GroupOrder, group);
			return index < 0 ? int.MaxValue : index;
		}

		public static string GetDisplayName(string group)
			=> group.GetLocalizedResource();

		private static (Dictionary<string, string> Groups, string[] GroupOrder, string[] ShownPropertyIds) LoadPropertyList()
		{
			var groups = new Dictionary<string, string>();
			var groupOrder = new List<string>();
			var shownPropertyIds = new List<string>();
			try
			{
				var path = SystemIO.Path.Combine(Package.Current.InstalledLocation.Path, "Assets", "Resources", "PropertiesInformation.json");
				using var document = JsonDocument.Parse(SystemIO.File.ReadAllText(path));
				foreach (var property in document.RootElement.EnumerateArray())
				{
					if (!property.TryGetProperty("SectionResource", out var section) || section.GetString() is not { Length: > 0 } group)
						continue;

					if (!groupOrder.Contains(group))
						groupOrder.Add(group);

					if (!property.TryGetProperty("Property", out var id) || id.GetString() is not { Length: > 0 } propertyId || !groups.TryAdd(propertyId, group))
						continue;

					if (!property.TryGetProperty("ShowInDetailsPage", out var showInDetailsPage) || showInDetailsPage.ValueKind is not JsonValueKind.False || property.TryGetProperty("PreviewPane", out _))
						shownPropertyIds.Add(propertyId);
				}
			}
			catch (Exception ex) when (ex is SystemIO.IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
			{
			}

			return (groups, [.. groupOrder], [.. shownPropertyIds]);
		}
	}
}
