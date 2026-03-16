// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Actions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.UserControls
{
	public sealed partial class Toolbar : UserControl
	{
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IModifiableCommandManager ModifiableCommands = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel? ViewModel { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowViewControlButton { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowPreviewPaneButton { get; set; }

		public Toolbar()
		{
			InitializeComponent();
		}

		private void NewEmptySpace_Opening(object sender, object e)
		{
			if (ViewModel is null)
				return;

			NewEmptySpace.Items.Clear();

			foreach (var item in CreateActionGroupMenuItems(Commands.Groups.NewItem))
				NewEmptySpace.Items.Add(item);

			NewEmptySpace.Items.Add(NewMenuFileFolderSeparator);

			if (!ViewModel.InstanceViewModel.CanCreateFileInPage)
				return;

			var cachedNewContextMenuEntries = addItemService.GetEntries();
			if (cachedNewContextMenuEntries is null || cachedNewContextMenuEntries.Count == 0)
				return;

			string keyFormat = $"D{cachedNewContextMenuEntries.Count.ToString().Length}";

			for (int index = 0; index < cachedNewContextMenuEntries.Count; index++)
			{
				var newEntry = cachedNewContextMenuEntries[index];
				var menuItem = CreateShellNewEntryMenuItem(newEntry);
				menuItem.AccessKey = (index + 1).ToString(keyFormat);
				menuItem.Command = ViewModel.CreateNewFileCommand;
				menuItem.CommandParameter = newEntry;
				NewEmptySpace.Items.Add(menuItem);
			}
		}

		private static MenuFlyoutItem CreateShellNewEntryMenuItem(ShellNewEntry newEntry)
		{
			if (!string.IsNullOrEmpty(newEntry.IconBase64))
			{
				byte[] bitmapData = Convert.FromBase64String(newEntry.IconBase64);
				using var ms = new MemoryStream(bitmapData);
				var image = new BitmapImage();
				_ = image.SetSourceAsync(ms.AsRandomAccessStream());

				return new MenuFlyoutItemWithImage
				{
					Text = newEntry.Name,
					BitmapIcon = image,
				};
			}

			return new MenuFlyoutItem
			{
				Text = newEntry.Name,
				Icon = new FontIcon
				{
					Glyph = "\xE7C3"
				},
			};
		}

		private void ActionGroupFlyout_Opening(object sender, object e)
		{
			if (sender is not MenuFlyout flyout)
				return;

			var group = flyout == ExtractFlyout ? Commands.Groups.Extract
				: flyout == SetAsFlyout ? Commands.Groups.SetAs
				: null;

			if (group is null)
				return;

			flyout.Items.Clear();
			foreach (var item in CreateActionGroupMenuItems(group))
				flyout.Items.Add(item);
		}

		private IEnumerable<MenuFlyoutItem> CreateActionGroupMenuItems(CommandGroup group)
			=> group.Commands
				.Select(code => Commands[code])
				.Where(command => command.Code != CommandCodes.None)
				.Select(CreateGroupMenuItem);

		private static MenuFlyoutItem CreateGroupMenuItem(IRichCommand command)
		{
			var item = new MenuFlyoutItem
			{
				Text = command.Label,
				Command = command,
				Visibility = command.IsExecutable ? Visibility.Visible : Visibility.Collapsed,
			};
			if (command.HotKeyText is string hotKey)
				item.KeyboardAcceleratorTextOverride = hotKey;
			var icon = command.Glyph.ToFontIcon();
			if (icon is not null)
				item.Icon = icon;
			return item;
		}

		private void SortGroup_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (sender is MenuFlyoutSubItem menu)
			{
				var items = menu.Items
					.TakeWhile(item => item is not MenuFlyoutSeparator)
					.Where(item => item.IsEnabled)
					.ToList();

				string format = $"D{items.Count.ToString().Length}";

				for (ushort index = 0; index < items.Count; ++index)
				{
					items[index].AccessKey = (index + 1).ToString(format);
				}
			}

		}

		private void AppBarButton_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
		}

		private void LayoutButton_Click(object sender, RoutedEventArgs e)
		{
			// Hide flyout after choosing a layout
			// Check if LayoutFlyout is not null to handle cases where UI elements are unloaded via x:Load
			LayoutFlyout?.Hide();
		}
	}
}
