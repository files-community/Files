// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Controls;
using Files.App.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

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
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}

		public void NavigateTo(SettingsNavigationParams navParams)
		{
			var defaultTag = SettingsPageKind.AppearancePage.ToString();
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

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
		}

		private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		public class SearchResultItem
		{
			public string Text { get; set; }
			public Page ParentPage { get; set; }
			public override string ToString() => Text;  // Ensures dropdown shows search text
		}

		private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				var query = sender.Text;
				var results = SearchSettingsPages(query);

				// Store actual objects instead of just text
				sender.ItemsSource = results;
			}
		}

		private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			var query = args.QueryText;
			var results = SearchSettingsPages(query);
			sender.ItemsSource = results;
		}

		private void NavViewSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is SearchResultItem selectedItem)
			{
				SettingsContentFrame.Navigate(selectedItem.ParentPage.GetType());
				NavViewSearchBox.Text = string.Empty;
			}
		}

		private IEnumerable<string> SearchVisualTreeForText(DependencyObject parent, string query)
		{
			var results = new List<string>();

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is FrameworkElement element)
				{
					if (element is TextBlock textBlock && textBlock.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
						results.Add(textBlock.Text);
					else if (element is SettingsCard settingsCard && settingsCard.Header is string headerText &&
							 headerText.Contains(query, StringComparison.OrdinalIgnoreCase))
						results.Add(headerText);
					else if (element is SettingsExpander settingsExpander)
					{
						if (settingsExpander.Header is string expanderHeader &&
							expanderHeader.Contains(query, StringComparison.OrdinalIgnoreCase))
							results.Add(expanderHeader);

						results.AddRange(SearchVisualTreeForText(settingsExpander, query));

						results.AddRange(settingsExpander.Items.OfType<SettingsCard>()
							.Select(subCard => subCard.Header)
							.OfType<string>()
							.Where(subCardHeader => subCardHeader.Contains(query, StringComparison.OrdinalIgnoreCase)));
					}
				}

				results.AddRange(SearchVisualTreeForText(child, query));
			}

			return results;
		}


		private IEnumerable<SearchResultItem> SearchSettingsPages(string query)
		{
			var results = new List<SearchResultItem>();

			var settingsPages = new List<Page>
			{
				new AboutPage(),
				new AppearancePage(),
				new TagsPage(),
				new DevToolsPage(),
				new GeneralPage(),
				new FoldersPage(),
			};

			foreach (var page in settingsPages)
			{
				var matches = SearchVisualTreeForText(page, query);
				foreach (var match in matches)
				{
					results.Add(new SearchResultItem { Text = match, ParentPage = page });
				}
			}

			return results;
		}

	}
}
