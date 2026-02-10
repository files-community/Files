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
using Windows.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation;

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
				var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Constants.ResourceFilePaths.SettingsStringKeysJsonPath));
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
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				var query = sender.Text?.Trim().ToLowerInvariant() ?? string.Empty;

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
		}

		private class SettingSuggestion
		{
			public string Key { get; set; }
			public string Localized { get; set; }
			public override string ToString() => Localized;
		}

		private void SettingsSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			// No action needed
		}

		private async void SettingsSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			var query = args.QueryText?.Trim().ToLowerInvariant() ?? string.Empty;

			if (args.ChosenSuggestion is SettingSuggestion suggestion && !string.IsNullOrEmpty(suggestion.Key))
			{
				if (keyToPage.TryGetValue(suggestion.Key, out var page))
				{
					await NavigateToPageByName(page, suggestion.Localized);
				}
			}
			else
			{
				var suggestions = keyToLocalized
					.Where(x => x.Value.ToLowerInvariant().Contains(query))
					.Select(x => new SettingSuggestion { Key = x.Key, Localized = x.Value })
					.DistinctBy(s => s.Localized)
					.ToList();

				if (suggestions.Count == 0)
				{
					var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
					string noResults = resourceLoader.GetString("NoResultsFound");
					sender.ItemsSource = new List<SettingSuggestion> {
					new SettingSuggestion { Key = null, Localized = noResults }
				};
				}
				else
				{
					sender.ItemsSource = suggestions;
				}
			}
		}

		private async Task NavigateToPageByName(string pageName, string? localizedName = null)
		{
			foreach (NavigationViewItem item in MainSettingsNavigationView.MenuItems)
			{
				if ((item.Tag as string) == pageName)
				{
					MainSettingsNavigationView.SelectedItem = item;
					break;
				}
			}

			if (localizedName is not null)
			{
				await Task.Delay(100); // Wait for UI to render
				var page = SettingsContentFrame.Content as FrameworkElement;
				if (page is not null)
				{
					var element = FindElementByText(page, localizedName);
					if (element is not null)
					{
						// Check if inside an Expander
						var expander = FindParent<Expander>(element);
						if (expander is not null && !expander.IsExpanded)
						{
							expander.IsExpanded = true;
							await Task.Delay(100); // Wait for animation
						}

						// Scroll to the element
						var transform = element.TransformToVisual(SettingsContentScrollViewer);
						var position = transform.TransformPoint(new Point(0, 0));
						SettingsContentScrollViewer.ChangeView(null, position.Y, null, false);
					}
				}
			}
		}

		private FrameworkElement? FindElementByText(FrameworkElement root, string text)
		{
			if (root is TextBlock tb && tb.Text == text)
				return root;
			if (AutomationProperties.GetName(root) == text)
				return root;
			if (root is ContentControl cc && cc.Content is string s && s == text)
				return root;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
			{
				var child = VisualTreeHelper.GetChild(root, i) as FrameworkElement;
				if (child is not null)
				{
					var found = FindElementByText(child, text);
					if (found is not null)
						return found;
				}
			}
			return null;
		}

		private T? FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			var parent = VisualTreeHelper.GetParent(child);
			while (parent is not null)
			{
				if (parent is T t)
					return t;
				parent = VisualTreeHelper.GetParent(parent);
			}
			return null;
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
