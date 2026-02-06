// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using System.Collections;

namespace Files.App.Dialogs
{
	public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
	{
		public SettingsDialogViewModel ViewModel { get; set; }

		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;





		public SettingsDialog()
		{
			InitializeComponent();

			MainWindow.Instance.SizeChanged += Current_SizeChanged;

			UpdateDialogLayout();
			LoadSettingsKeysAsync();

			// Ensure we can react when a page is navigated so we can scroll to a specific setting
			SettingsContentFrame.Navigated += SettingsContentFrame_Navigated;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}

		public void NavigateTo(SettingsNavigationParams navParams)
		{
			const string defaultTag = nameof(SettingsPageKind.AppearancePage);
			var oldSelection = MainSettingsNavigationView.MenuItems.FirstOrDefault(item => ((NavigationViewItem)item).IsSelected) as NavigationViewItem;
			var targetSection = MainSettingsNavigationView.MenuItems.FirstOrDefault(
				item => Enum.Parse<SettingsPageKind>(((NavigationViewItem)item).Tag.ToString() ?? defaultTag) == navParams.PageKind
			);
			if (oldSelection is not null)
				oldSelection.IsSelected = false;

			MainSettingsNavigationView.SelectedItem = targetSection;
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			ContainerGrid.Height = MainWindow.Instance.Bounds.Height <= 760 ? MainWindow.Instance.Bounds.Height - 70 : 690;
			ContainerGrid.Width = MainWindow.Instance.Bounds.Width <= 1100 ? MainWindow.Instance.Bounds.Width : 1100;

			var paneDisplayMode = MainWindow.Instance.Bounds.Width < 700
				? NavigationViewPaneDisplayMode.LeftCompact
				: NavigationViewPaneDisplayMode.Left;

			if (MainSettingsNavigationView.PaneDisplayMode != paneDisplayMode)
				MainSettingsNavigationView.PaneDisplayMode = paneDisplayMode;
		}

		private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			SettingsContentScrollViewer.ChangeView(null, 0, null, true);
			var selectedItem = (NavigationViewItem)args.SelectedItem;

			_ = Enum.Parse<SettingsPageKind>(selectedItem.Tag.ToString()) switch
			{
				SettingsPageKind.GeneralPage => SettingsContentFrame.Navigate(typeof(GeneralPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AppearancePage => SettingsContentFrame.Navigate(typeof(AppearancePage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.LayoutPage => SettingsContentFrame.Navigate(typeof(LayoutPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.FoldersPage => SettingsContentFrame.Navigate(typeof(FoldersPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.ActionsPage => SettingsContentFrame.Navigate(typeof(ActionsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.TagsPage => SettingsContentFrame.Navigate(typeof(TagsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.DevToolsPage => SettingsContentFrame.Navigate(typeof(DevToolsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AdvancedPage => SettingsContentFrame.Navigate(typeof(AdvancedPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AboutPage => SettingsContentFrame.Navigate(typeof(AboutPage), null, new SuppressNavigationTransitionInfo()),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage), null, new SuppressNavigationTransitionInfo())
			};
		}

		private Dictionary<string, List<string>> settingsKeysByPage = new();
		private Dictionary<string, string> keyToPage = new();
		private Dictionary<string, string> keyToLocalized = new();

		public async void LoadSettingsKeysAsync()
		{
			try
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Data/settings_string_keys.json"));
				using var stream = await file.OpenStreamForReadAsync();
				var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(stream);
				if (dict != null)
				{
					settingsKeysByPage = dict;
					keyToPage.Clear();
					keyToLocalized.Clear();
					var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
					foreach (var kvp in dict)
					{
						foreach (var key in kvp.Value)
						{
							keyToPage[key] = kvp.Key;
							string localized = resourceLoader.GetString(key);
							if (string.IsNullOrEmpty(localized))
								localized = key;
							keyToLocalized[key] = localized;
						}
					}
				}
			}
			catch { }
		}

		private void SettingsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
				return;

			var query = sender.Text?.Trim().ToLowerInvariant() ?? string.Empty;

			// Debounce: Only update suggestions if query changed
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
			string noResults = resourceLoader.GetString("NoResultsFound");
			if (string.IsNullOrEmpty(query))
			{
				// Show a placeholder for empty search
				sender.ItemsSource = new List<SettingSuggestion> {
					new SettingSuggestion { Key = null, Localized = noResults }
				};
				return;
			}

			var suggestions = keyToLocalized
				.Where(kvp => kvp.Value.ToLowerInvariant().Contains(query))
				.Select(kvp => new SettingSuggestion { Key = kvp.Key, Localized = kvp.Value })
				.DistinctBy(s => s.Localized)
				.ToList();

			if (suggestions.Count == 0)
			{
				// Show a placeholder for no results
				sender.ItemsSource = new List<SettingSuggestion> {
					new SettingSuggestion { Key = null, Localized = noResults }
				};
			}
			else
			{
				sender.ItemsSource = suggestions;
			}
		}

		private class SettingSuggestion
		{
			public string Key { get; set; }
			public string Localized { get; set; }
			public override string ToString() => Localized;
		}

		private void SettingsSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is SettingSuggestion suggestion && !string.IsNullOrEmpty(suggestion.Key))
			{
				if (keyToPage.TryGetValue(suggestion.Key, out var page))
				{
					// Store which key we want to scroll to after navigation
					_pendingSettingKey = suggestion.Key;
					NavigateToPageByName(page);
					// If the page is already selected, try to scroll immediately
					_ = AttemptImmediateScrollIfPageActive(page);
				}
			}

			// Close the suggestion dropdown
			sender.IsSuggestionListOpen = false;
			// Clear the query after a suggestion is chosen
			sender.Text = string.Empty;
		}

		private void SettingsSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			var query = args.QueryText?.Trim().ToLowerInvariant() ?? string.Empty;
			if (string.IsNullOrEmpty(query))
			{
				// Close dropdown and clear text
				sender.IsSuggestionListOpen = false;
				sender.Text = string.Empty;
				return;
			}

			var match = keyToLocalized.FirstOrDefault(x => x.Value.ToLowerInvariant().Contains(query));
			if (!string.IsNullOrEmpty(match.Key) && keyToPage.TryGetValue(match.Key, out var page))
			{
				_pendingSettingKey = match.Key;
				NavigateToPageByName(page);
				// If the page is already selected, try to scroll immediately
				_ = AttemptImmediateScrollIfPageActive(page);
			}

			// Close dropdown and clear text
			sender.IsSuggestionListOpen = false;
			sender.Text = string.Empty;
		}

		private void NavigateToPageByName(string pageName)
		{
			foreach (NavigationViewItem item in MainSettingsNavigationView.MenuItems)
			{
				if ((item.Tag as string) == pageName)
				{
					MainSettingsNavigationView.SelectedItem = item;
					break;
				}
			}
		}

		private string _pendingSettingKey;

		private async void SettingsContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			// Small delay to allow page controls to load into the visual tree
			await Task.Delay(80);
			if (string.IsNullOrEmpty(_pendingSettingKey))
				return;

			// Try to scroll to the pending setting; swallow failures silently
			try
			{
				await ScrollToPendingSettingAsync(_pendingSettingKey);
			}
			catch { }
			// Clear pending key after attempt
			_pendingSettingKey = null;
		}

		private async Task ScrollToPendingSettingAsync(string key)
		{
			if (!keyToLocalized.TryGetValue(key, out var localized))
				localized = key;

			var content = SettingsContentFrame.Content as DependencyObject;
			if (content is null)
				return;

			// Wait a bit more to ensure children are loaded
			await Task.Delay(40);

			// First try: find a SettingsExpander that contains the matching element (handles case
			// where expander header text differs from inner setting header).
			var containingExpander = FindContainingExpander(content, localized);
			FrameworkElement elementToBring = null;
			if (containingExpander != null)
			{
				var prop = containingExpander.GetType().GetProperty("IsExpanded");
				if (prop != null && prop.PropertyType == typeof(bool))
					prop.SetValue(containingExpander, true);
				// allow expand animation to complete and children to load
				await Task.Delay(60);
				if (containingExpander is FrameworkElement feExp)
					elementToBring = feExp;
			}
			// Fallback: try to find the element anywhere in the page
			if (elementToBring == null)
			{
				var target = FindElementByHeader(content, localized);
				if (target == null)
					return;
				// If inside an expander, expand its nearest ancestor and prefer scrolling to that expander
				var expander = FindAncestorByTypeName(target, "SettingsExpander");
				elementToBring = target as FrameworkElement;
				if (expander != null)
				{
					var prop = expander.GetType().GetProperty("IsExpanded");
					if (prop != null && prop.PropertyType == typeof(bool))
						prop.SetValue(expander, true);
					await Task.Delay(60);
					if (expander is FrameworkElement feExp)
						elementToBring = feExp;
				}
			}

			// Compute position relative to scrollviewer and scroll
			try
			{
				if (elementToBring == null)
					return;
				var transform = elementToBring.TransformToVisual(SettingsContentScrollViewer as UIElement);
				var point = transform.TransformPoint(new Point(0, 0));
				// Add current offset so we get absolute target position
				var y = point.Y + SettingsContentScrollViewer.VerticalOffset;
				SettingsContentScrollViewer.ChangeView(null, y, null, true);
			}
			catch
			{
				// fallback to BringIntoView
				if (elementToBring is UIElement ui)
					ui.StartBringIntoView();
			}
		}

		private DependencyObject FindElementByHeader(DependencyObject root, string localized)
		{
			if (root == null)
				return null;

			// Check current element
			try
			{
				// Check AutomationProperties.Name
				var apName = (string)root.GetValue(AutomationProperties.NameProperty);
				if (!string.IsNullOrEmpty(apName) && apName.Equals(localized, StringComparison.OrdinalIgnoreCase))
					return root;

				// Check TextBlock.Text directly (often used in templates for headers)
				if (root is TextBlock tb)
				{
					if (!string.IsNullOrEmpty(tb.Text) && tb.Text.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
						return root;
				}

				// Check for Header, Content or other string properties via reflection (SettingsCard, SettingsExpander, etc.)
				var t = root.GetType();
				var stringProps = new[] { "Header", "HeaderText", "Content", "Text" };
				foreach (var propName in stringProps)
				{
					var p = t.GetProperty(propName);
					if (p == null)
						continue;
					var val = p.GetValue(root);
					if (val is string vs && !string.IsNullOrEmpty(vs) && vs.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
						return root;
					if (val != null && val.ToString().IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
						return root;
				}
			}
			catch { }

			// Recurse children
			int count = VisualTreeHelper.GetChildrenCount(root);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				var found = FindElementByHeader(child, localized);
				if (found != null)
					return found;
			}

			return null;
		}

		private DependencyObject FindAncestorByTypeName(DependencyObject start, string typeName)
		{
			var parent = VisualTreeHelper.GetParent(start);
			while (parent != null)
			{
				if (parent.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
					return parent;
				parent = VisualTreeHelper.GetParent(parent);
			}
			return null;
		}

		private bool TryGetStringPropertyValue(object obj, string propertyName, out string value)
		{
			value = null;
			if (obj == null)
				return false;
			try
			{
				var p = obj.GetType().GetProperty(propertyName);
				if (p == null)
					return false;
				var v = p.GetValue(obj);
				if (v == null)
					return false;
				value = v.ToString();
				return true;
			}
			catch { return false; }
		}

		private bool MatchesHeader(DependencyObject element, string localized)
		{
			if (element == null)
				return false;
			try
			{
				// AutomationProperties.Name
				var apName = (string)element.GetValue(AutomationProperties.NameProperty);
				if (!string.IsNullOrEmpty(apName) && apName.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
					return true;

				// TextBlock.Text
				if (element is TextBlock tb && !string.IsNullOrEmpty(tb.Text) && tb.Text.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
					return true;

				// Common string properties
				var props = new[] { "Header", "HeaderText", "Content", "Text", "Name" };
				foreach (var pn in props)
				{
					if (TryGetStringPropertyValue(element, pn, out var val) && !string.IsNullOrEmpty(val) && val.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
						return true;
				}
			}
			catch { }
			return false;
		}

		private bool ExpanderHasItemWithHeader(DependencyObject expander, string localized)
		{
			if (expander == null)
				return false;
			try
			{
				// Do not treat expander header as an item match here; only inspect items

				// Inspect logical items via Items property if present
				if (TryGetStringPropertyValue(expander, "Items", out _))
				{
					var itemsProp = expander.GetType().GetProperty("Items");
					var items = itemsProp?.GetValue(expander) as IEnumerable;
					if (items != null)
					{
						foreach (var item in items)
						{
							if (item == null)
								continue;
							if (item is DependencyObject dobj && MatchesHeader(dobj, localized))
								return true;
							// check simple string or common props
							var s = item.ToString();
							if (!string.IsNullOrEmpty(s) && s.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
								return true;
							var it = item.GetType();
							foreach (var pn in new[] { "Header", "Content", "Text", "Name" })
							{
								var p = it.GetProperty(pn);
								if (p == null)
									continue;
								var v = p.GetValue(item);
								if (v != null && v.ToString().IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0)
									return true;
							}
						}
					}
				}
			}
			catch { }
			return false;
		}

		private void SetExpanderIsExpanded(DependencyObject expander, bool isExpanded)
		{
			if (expander == null)
				return;
			try
			{
				var prop = expander.GetType().GetProperty("IsExpanded");
				if (prop != null && prop.PropertyType == typeof(bool))
					prop.SetValue(expander, isExpanded);
			}
			catch { }
		}

		private DependencyObject FindContainingExpander(DependencyObject root, string localized)
		{
			if (root == null)
				return null;
			int count = VisualTreeHelper.GetChildrenCount(root);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child == null)
					continue;
				// If this child is a SettingsExpander, search inside it
				if (child.GetType().Name.Equals("SettingsExpander", StringComparison.OrdinalIgnoreCase))
				{
					// Try to detect the match by inspecting the expander and its logical items
					if (ExpanderHasItemWithHeader(child, localized))
						return child;
					// Fallback: attempt to find in visual subtree of the expander
					var found = FindElementByHeader(child, localized);
					// Only treat this expander as containing the match if the found element
					// is not the expander's header itself. This prevents expanding the
					// expander when the search hit is the header.
					if (found != null && !MatchesHeader(child, localized))
						return child;
				}
				// Recurse deeper
				var deeper = FindContainingExpander(child, localized);
				if (deeper != null)
					return deeper;
			}
			return null;
		}

		private async Task AttemptImmediateScrollIfPageActive(string pageName)
		{
			try
			{
				if (MainSettingsNavigationView.SelectedItem is not NavigationViewItem sel)
					return;
				if ((sel.Tag as string) != pageName)
					return;

				// Small delay to allow any UI updates to settle
				await Task.Delay(40);
				if (string.IsNullOrEmpty(_pendingSettingKey))
					return;
				await ScrollToPendingSettingAsync(_pendingSettingKey);
			}
			catch { }
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
		}

		private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}
	}
}
