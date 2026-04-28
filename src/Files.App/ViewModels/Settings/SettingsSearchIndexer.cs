// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Controls;
using Files.App.Views.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.Settings
{
	internal static class SettingsSearchIndexer
	{
		public static List<SettingsSearchResult> BuildIndex()
		{
			var results = new List<SettingsSearchResult>();

			var pages = new (SettingsPageKind Kind, string DisplayName, Func<Page> Factory)[]
			{
				(SettingsPageKind.GeneralPage,    Strings.General.GetLocalizedResource(),         () => new GeneralPage()),
				(SettingsPageKind.AppearancePage, Strings.Appearance.GetLocalizedResource(),      () => new AppearancePage()),
				(SettingsPageKind.LayoutPage,     Strings.Layout.GetLocalizedResource(),          () => new LayoutPage()),
				(SettingsPageKind.FoldersPage,    Strings.FilesAndFolders.GetLocalizedResource(), () => new FoldersPage()),
				(SettingsPageKind.ActionsPage,    Strings.Actions.GetLocalizedResource(),         () => new ActionsPage()),
				(SettingsPageKind.TagsPage,       Strings.FileTags.GetLocalizedResource(),        () => new TagsPage()),
				(SettingsPageKind.DevToolsPage,   Strings.DevTools.GetLocalizedResource(),        () => new DevToolsPage()),
				(SettingsPageKind.AdvancedPage,   Strings.Advanced.GetLocalizedResource(),        () => new AdvancedPage()),
				(SettingsPageKind.AboutPage,      Strings.About.GetLocalizedResource(),           () => new AboutPage()),
			};

			foreach (var (kind, name, factory) in pages)
			{
				try
				{
					var page = factory();
					Walk(page.Content, kind, name, parentHeader: null, results);
					(page.DataContext as IDisposable)?.Dispose();
				}
				catch
				{
				}
			}

			return results;
		}

		private static void Walk(object? node, SettingsPageKind kind, string pageName, string? parentHeader, List<SettingsSearchResult> results)
		{
			switch (node)
			{
				case SettingsExpander expander when expander.Header is string groupHeader && !string.IsNullOrWhiteSpace(groupHeader):
					results.Add(new SettingsSearchResult(kind, pageName, groupHeader));
					foreach (var item in expander.Items)
						Walk(item, kind, pageName, groupHeader, results);
					return;

				case SettingsCard card when card.Header is string cardHeader && !string.IsNullOrWhiteSpace(cardHeader):
					results.Add(new SettingsSearchResult(kind, pageName, cardHeader, parentHeader));
					return;

				case Panel panel:
					foreach (var child in panel.Children)
						Walk(child, kind, pageName, parentHeader, results);
					return;
			}
		}
	}
}
