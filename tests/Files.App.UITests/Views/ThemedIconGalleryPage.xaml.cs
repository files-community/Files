// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UITests.Views
{
	public sealed class IconGalleryEntry
	{
		public string Key { get; set; } = string.Empty;
		public string ShortName { get; set; } = string.Empty;
		public Style? IconStyle { get; set; }
	}

	public sealed partial class ThemedIconGalleryPage : Page
	{
		private const string IconKeyPrefix = "App.ThemedIcons.";
		private readonly IReadOnlyList<IconGalleryEntry> allIcons;

		public ObservableCollection<IconGalleryEntry> FilteredIcons { get; } = [];

		public ThemedIconGalleryPage()
		{
			InitializeComponent();
			allIcons = BuildEntries();
			ApplyFilter(string.Empty);
		}

		private static IReadOnlyList<IconGalleryEntry> BuildEntries()
		{
			var iconStyles = new Dictionary<string, Style>(StringComparer.Ordinal);
			CollectIconStyles(Application.Current.Resources, iconStyles);

			return iconStyles
				.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.Select(x => new IconGalleryEntry
				{
					Key = x.Key,
					ShortName = x.Key[IconKeyPrefix.Length..],
					IconStyle = x.Value,
				})
				.ToList();
		}

		private static void CollectIconStyles(ResourceDictionary dictionary, IDictionary<string, Style> iconStyles)
		{
			foreach (var key in dictionary.Keys)
			{
				if (key is not string resourceKey ||
					!resourceKey.StartsWith(IconKeyPrefix, StringComparison.Ordinal) ||
					!dictionary.TryGetValue(key, out var resourceValue) ||
					resourceValue is not Style style)
				{
					continue;
				}

				iconStyles[resourceKey] = style;
			}

			foreach (var mergedDictionary in dictionary.MergedDictionaries)
			{
				CollectIconStyles(mergedDictionary, iconStyles);
			}

			foreach (var themeDictionary in dictionary.ThemeDictionaries)
			{
				if (themeDictionary.Value is ResourceDictionary themedResourceDictionary)
				{
					CollectIconStyles(themedResourceDictionary, iconStyles);
				}
			}
		}

		private void ApplyFilter(string query)
		{
			FilteredIcons.Clear();
			var trimmed = query.Trim();

			foreach (var entry in allIcons)
			{
				if (string.IsNullOrEmpty(trimmed) ||
					entry.Key.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
				{
					FilteredIcons.Add(entry);
				}
			}
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilter(SearchBox.Text);
		}

		private async void IconButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button { Tag: string key })
				return;

			var package = new DataPackage();
			package.SetText(key);
			Clipboard.SetContent(package);

			CopiedInfoBar.Message = $"Copied: {key}";
			CopiedInfoBar.IsOpen = true;

			await System.Threading.Tasks.Task.Delay(2500);
			CopiedInfoBar.IsOpen = false;
		}
	}
}
