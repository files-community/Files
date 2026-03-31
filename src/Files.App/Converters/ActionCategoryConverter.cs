// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Converters
{
	public static class ActionCategoryConverter
	{
		public static string ToLocalizedCategoryPath(ActionCategory category)
			=> category switch
			{
				ActionCategory.Archive => Strings.Archive.GetLocalizedResource(),
				ActionCategory.Display => Strings.Display.GetLocalizedResource(),
				ActionCategory.Edit => Strings.Edit.GetLocalizedResource(),
				ActionCategory.FileSystem => Strings.FileSystem.GetLocalizedResource(),
				ActionCategory.Grouping => Strings.GroupBy.GetLocalizedResource(),
				ActionCategory.Git => Strings.Git.GetLocalizedResource(),
				ActionCategory.Image => Strings.PropertySectionImage.GetLocalizedResource(),
				ActionCategory.Install => Strings.Install.GetLocalizedResource(),
				ActionCategory.Layout => Strings.Layout.GetLocalizedResource(),
				ActionCategory.Media => Strings.PropertySectionMedia.GetLocalizedResource(),
				ActionCategory.Navigation => Strings.Navigation.GetLocalizedResource(),
				ActionCategory.Create => Strings.Create.GetLocalizedResource(),
				ActionCategory.Open => Strings.Open.GetLocalizedResource(),
				ActionCategory.Run => Strings.Run.GetLocalizedResource(),
				ActionCategory.Selection => Strings.Selection.GetLocalizedResource(),
				ActionCategory.Show => Strings.Show.GetLocalizedResource(),
				ActionCategory.Sorting => Strings.SortBy.GetLocalizedResource(),
				ActionCategory.Start => Strings.Start.GetLocalizedResource(),
				ActionCategory.Window => Strings.Window.GetLocalizedResource(),
				ActionCategory.DualPane => Strings.DualPaneCategory.GetLocalizedResource(),
				_ => Strings.General.GetLocalizedResource(),
			};
	}
}