// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Files.App.UITests.Views
{
	public sealed partial class OmnibarPage : Page
	{
		private readonly string Omnibar1_TextMemberPathForPaletteMode = nameof(OmnibarPaletteSuggestionItem.Title);

		private readonly ObservableCollection<OmnibarPaletteSuggestionItem> Omnibar1_PaletteSuggestions;
		private readonly ObservableCollection<BreadcrumbBarItemModel> Omnibar1_BreadcrumbBarItems;

		[GeneratedDependencyProperty(DefaultValue = "")]
		private partial string Omnibar1_Text { get; set; }

		[GeneratedDependencyProperty(DefaultValue = "")]
		private partial string Omnibar1_TextChangedReason { get; set; }

		[GeneratedDependencyProperty]
		private partial int Omnibar1_ChosenSuggestionIndex { get; set; }

		[GeneratedDependencyProperty(DefaultValue = "")]
		private partial string Omnibar1_SubmittedQuery { get; set; }

		public OmnibarPage()
		{
			InitializeComponent();

			Omnibar1_PaletteSuggestions =
			[
				new("Open online help page in browser", "Open online help page in browser", "Control + H"),
				new("Toggle full screen", "Toggle full screen", "Control + H"),
				new("Enter compact overlay", "Enter compact overlay", "Control + H"),
				new("Toggle compact overlay", "Toggle compact overlay", "Control + H"),
				new("Go to search box", "Go to search box", "Control + H"),
				new("Focus path bar", "Focus path bar", "Control + H"),
				new("Redo the last file operation", "Redo the last file operation", "Control + H"),
				new("Undo the last file operation", "Undo the last file operation", "Control + H"),
				new("Toggle whether to show hidden items", "Toggle whether to show hidden items", "Control + H"),
			];

			Omnibar1_BreadcrumbBarItems =
			[
				new("Local Disk (C:)"),
				new("Users"),
				new("me"),
				new("OneDrive"),
				new("Desktop"),
				new("Folder1"),
				new("Folder2"),
			];
		}

		private void Omnibar1_BreadcrumbBar_ItemDropDownFlyoutOpening(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 1" });
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 2" });
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 3" });
		}

		private void Omnibar1_BreadcrumbBar_ItemDropDownFlyoutClosed(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			e.Flyout.Items.Clear();
		}

		private void Omnibar1_QuerySubmitted(Omnibar sender, OmnibarQuerySubmittedEventArgs args)
		{
			Omnibar1_ChosenSuggestionIndex = args.Item is OmnibarPaletteSuggestionItem item ? Omnibar1_PaletteSuggestions.IndexOf(item) : -1;
			Omnibar1_SubmittedQuery = args.Mode.Text ?? string.Empty;
		}

		private void Omnibar1_TextChanged(Omnibar sender, OmnibarTextChangedEventArgs args)
		{
			if (args.Reason is not OmnibarTextChangeReason.SuggestionChosen)
				Omnibar1_ChosenSuggestionIndex = -1;

			Omnibar1_Text = args.Mode.Text ?? string.Empty;
			Omnibar1_TextChangedReason = args.Reason.ToString();
		}

		private void Omnibar1_SuggestionChosen(Omnibar sender, OmnibarSuggestionChosenEventArgs args)
		{
			Omnibar1_ChosenSuggestionIndex = args.SelectedItem is OmnibarPaletteSuggestionItem item ? Omnibar1_PaletteSuggestions.IndexOf(item) : -1;
			Omnibar1_SubmittedQuery = args.Mode.Text ?? string.Empty;
		}
	}
}
