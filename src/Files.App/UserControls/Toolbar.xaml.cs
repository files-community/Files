// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using FlyoutPlacementMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode;

namespace Files.App.UserControls
{
	public sealed partial class Toolbar : UserControl
	{
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IModifiableCommandManager ModifiableCommands = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
		private readonly DispatcherQueueTimer toolbarRefreshTimer;
		private readonly IContentPageContext PageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private UserControls.Menus.FileTagsContextMenu? editTagsMenu;
		private OpenWithMenu? openWithMenu;
		private int openWithFlyoutRequestId;
		private readonly List<Action> toggleButtonDetachActions = new();

		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel? ViewModel { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowPreviewPaneButton { get; set; }

		public Toolbar()
		{
			toolbarRefreshTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			InitializeComponent();
			Loaded += Toolbar_Loaded;
			Unloaded += Toolbar_Unloaded;
		}

		private void Toolbar_Loaded(object sender, RoutedEventArgs e)
		{
			foreach (var cmd in Commands) cmd.PropertyChanged += Command_PropertyChanged;
			RequestToolbarRefresh(true);
			UserSettingsService.AppearanceSettingsService.PropertyChanged += AppearanceSettings_PropertyChanged;
		}

		private void Toolbar_Unloaded(object sender, RoutedEventArgs e)
		{
			foreach (var cmd in Commands) cmd.PropertyChanged -= Command_PropertyChanged;
			DetachToggleButtons();
			UserSettingsService.AppearanceSettingsService.PropertyChanged -= AppearanceSettings_PropertyChanged;
			if (editTagsMenu is not null)
				editTagsMenu.TagsChanged -= EditTagsMenu_TagsChanged;
			openWithMenu?.Dispose();
		}

		partial void OnViewModelChanged(NavigationToolbarViewModel? newValue)
		{
			if (newValue?.InstanceViewModel is not null)
			{
				newValue.InstanceViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
				RequestToolbarRefresh(true);
			}
		}

		private void Command_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is not nameof(IRichCommand.IsExecutable) || sender is not IRichCommand cmd
				|| cmd.Code is CommandCodes.None || !cmd.IsAccessibleGlobally)
				return;

			var contextId = ToolbarItemDescriptor.ResolveToolbarSectionId(cmd.Code.ToString(), Commands);
			if (contextId is ToolbarDefaultsTemplate.AlwaysVisibleContextId or ToolbarDefaultsTemplate.OtherContextsContextId)
				return;

			// Debouncing in RequestToolbarRefresh() coalesces rapid IsExecutable changes,
			// so we don't need to check if the context visibility actually changed here.
			// Checking IsContextActive() loops through all commands and is expensive during
			// file selection when many commands' executability changes rapidly.
			RequestToolbarRefresh(false);
		}

		private void InstanceViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(CurrentInstanceViewModel.IsPageTypeNotHome)
				or nameof(CurrentInstanceViewModel.IsPageTypeRecycleBin))
				RequestToolbarRefresh(false);
		}

		private void AppearanceSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAppearanceSettingsService.CustomToolbarItems))
				RequestToolbarRefresh(true);
		}

		private async void EditTagsMenu_TagsChanged(object? sender, EventArgs e)
		{
			if (PageContext.ShellPage is { } shellPage)
			{
				var shellViewModel = shellPage.GetRequiredShellViewModel();
				await shellViewModel.RefreshTagGroups();
			}
		}

		private void RequestToolbarRefresh(bool ignoreDebounce)
		{
			toolbarRefreshTimer.Debounce(PopulateToolbarItems, TimeSpan.FromMilliseconds(100), ignoreDebounce);
		}

		private void ContextCommandBar_Loaded(object sender, RoutedEventArgs e)
			=> RequestToolbarRefresh(true);

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(AppBarSeparator))]
		private void PopulateToolbarItems()
		{
			if (ContextCommandBar is null)
				return;

			DetachToggleButtons();
			ContextCommandBar.PrimaryCommands.Clear();

			var active = GetActiveToolbarContexts();
			var itemsByContext = ToolbarDefaultsTemplate.ResolveToolbarItemsByContext(UserSettingsService.AppearanceSettingsService);

			foreach (var contextId in ToolbarDefaultsTemplate.ContextOrder)
			{
				if (!itemsByContext.TryGetValue(contextId, out var entries) || entries.Count == 0
					|| !ShouldShowContext(contextId, active))
					continue;

				if (contextId != ToolbarDefaultsTemplate.AlwaysVisibleContextId)
					ContextCommandBar.PrimaryCommands.Add(new AppBarSeparator());

				for (int i = 0; i < entries.Count; i++)
				{
					if (CreateToolbarElement(entries[i]) is { } el)
					{
						if (el is FrameworkElement btn and not AppBarSeparator && !ToolbarItemDescriptor.IsSeparatorCode(entries[i].CommandCode ?? ""))
							AttachContextFlyout(btn, contextId, entries[i], i);

						ContextCommandBar.PrimaryCommands.Add(el);
					}
				}
			}

			UpdateCommandBarSeparatorVisibility(ContextCommandBar.PrimaryCommands);
		}

		private HashSet<string> GetActiveToolbarContexts()
		{
			var active = new HashSet<string>(StringComparer.Ordinal) { ToolbarDefaultsTemplate.AlwaysVisibleContextId };
			foreach (var cmd in Commands)
			{
				if (cmd.Code is CommandCodes.None || !cmd.IsAccessibleGlobally || !cmd.IsExecutable) continue;
				var ctxId = ToolbarItemDescriptor.ResolveToolbarSectionId(cmd.Code.ToString(), Commands);
				if (ctxId == ToolbarDefaultsTemplate.RecycleBinContextId && ViewModel?.InstanceViewModel?.IsPageTypeRecycleBin != true)
					continue;
				active.Add(ctxId);
			}
			return active;
		}

		private static bool ShouldShowContext(string contextId, ISet<string> active)
			=> contextId == ToolbarDefaultsTemplate.OtherContextsContextId
				? !active.Any(c => c is not ToolbarDefaultsTemplate.AlwaysVisibleContextId and not ToolbarDefaultsTemplate.OtherContextsContextId)
				: active.Contains(contextId);

		[DynamicWindowsRuntimeCast(typeof(Style))]
		private ICommandBarElement? CreateToolbarElement(ToolbarItemSettingsEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.CommandCode) && ToolbarItemDescriptor.IsSeparatorCode(entry.CommandCode))
				return new AppBarSeparator();

			var (showIcon, showLabel) = (entry.ShowIcon, entry.ShowLabel);

			if (!string.IsNullOrEmpty(entry.CommandGroup)
				&& Commands.Groups.All.FirstOrDefault(g => g.Name == entry.CommandGroup) is { } group)
			{
				showIcon &= !group.Glyph.IsNone;

				if (!showIcon && !showLabel)
					return null;

				var btn = CreateButton(showIcon, showLabel, group.DisplayName, group.DisplayName,
					group.AccessKey, group.AutomationId, group.Glyph);

				if (group is EditTagsCommandGroup)
				{
					if (editTagsMenu is null)
					{
						editTagsMenu = new UserControls.Menus.FileTagsContextMenu(() => PageContext.SelectedItems)
						{
							Placement = FlyoutPlacementMode.Bottom
						};
						editTagsMenu.TagsChanged += EditTagsMenu_TagsChanged;
					}
					btn.Flyout = editTagsMenu;
					btn.IsEnabled = PageContext.SelectedItems.Count > 0
						&& PageContext.PageType is not ContentPageTypes.Home
							and not ContentPageTypes.RecycleBin
							and not ContentPageTypes.None;
				}
				else
				{
					var menuFlyout = new MenuFlyout
					{
						Placement = group is NewItemCommandGroup or OpenWithCommandGroup
						? FlyoutPlacementMode.BottomEdgeAlignedLeft
						: FlyoutPlacementMode.Bottom
					};
					btn.Flyout = menuFlyout;

					menuFlyout.Opening += async (s, _) =>
					{
						try
						{
							await PopulateGroupFlyoutAsync(menuFlyout, group);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(ex);
						}
					};

					btn.IsEnabled = group.Commands.Any(c => c is not CommandCodes.None && Commands[c].IsExecutable);
				}

				btn.Style = (Style)Resources["ToolBarAppBarButtonFlyoutStyle"];

				if (showIcon)
					ApplyIcon(btn, group.Glyph, setContent: true);

				return btn;
			}

			if (!string.IsNullOrEmpty(entry.CommandCode) && Enum.TryParse<CommandCodes>(entry.CommandCode, out var code) && code != CommandCodes.None)
			{
				var mod = ModifiableCommands[code];
				var cmd = mod.Code != CommandCodes.None ? mod : Commands[code];
				showIcon &= !cmd.Glyph.IsNone;

				if (!showIcon && !showLabel)
					return null;

				if (cmd.IsToggle)
					return CreateToggleButton(cmd, showIcon, showLabel);

				var tooltip = cmd.HotKeyText is null ? cmd.ExtendedLabel : $"{cmd.ExtendedLabel} ({cmd.HotKeyText})";
				var btn = CreateButton(showIcon, showLabel, cmd.ExtendedLabel, tooltip,
					cmd.AccessKey, cmd.AutomationId, cmd.Glyph, cmd.HotKeyText);
				var useStyled = showIcon && !string.IsNullOrEmpty(cmd.Glyph.ThemedIconStyle);

				if (useStyled)
					btn.Style = (Style)Resources["ToolBarAppBarButtonFlyoutStyle"];

				if (showIcon)
					ApplyIcon(btn, cmd.Glyph, setContent: useStyled);

				btn.Command = cmd;

				return btn;
			}

			return null;
		}

		internal static ICommandBarElement? CreatePreviewElement(ToolbarItemDescriptor item, Style flyoutStyle)
		{
			if (item.IsSeparator)
				return new AppBarSeparator();

			var showIcon = item.ShowIcon && item.HasIcon;

			if (!showIcon && !item.ShowLabel)
				return null;

			var useStyled = item.IsGroup || (showIcon && !string.IsNullOrEmpty(item.Glyph.ThemedIconStyle));
			var button = new AppBarButton
			{
				Width = double.NaN,
				MinWidth = showIcon ? 40 : 0,
				Label = item.IsGroup ? item.DisplayName : item.ExtendedDisplayName,
				LabelPosition = item.ShowLabel ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed,
				IsHitTestVisible = false,
				Style = useStyled ? flyoutStyle : null,
				Flyout = item.IsGroup ? new MenuFlyout() : null,
			};

			if (showIcon)
				ApplyIcon(button, item.Glyph, setContent: useStyled);
			else
				button.Loaded += CollapseIconViewbox;

			return button;
		}

		private AppBarButton CreateButton(bool showIcon, bool showLabel, string label, string tooltip,
			string? accessKey, string? automationId, RichGlyph glyph, string? hotKeyText = null)
		{
			var button = new AppBarButton
			{
				Width = double.NaN,
				MinWidth = showIcon ? 40 : 0,
				Label = label,
				LabelPosition = showLabel ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed,
			};

			if (!showIcon)
				button.Loaded += CollapseIconViewbox;

			ToolTipService.SetToolTip(button, tooltip);

			if (!string.IsNullOrEmpty(accessKey))
			{
				button.AccessKey = accessKey;
				button.AccessKeyInvoked += AppBarButton_AccessKeyInvoked;
			}

			if (!string.IsNullOrEmpty(automationId))
				AutomationProperties.SetAutomationId(button, automationId);

			if (hotKeyText is not null)
				button.KeyboardAcceleratorTextOverride = hotKeyText;

			return button;
		}

		[DynamicWindowsRuntimeCast(typeof(AppBarToggleButton))]
		private AppBarToggleButton CreateToggleButton(IRichCommand cmd, bool showIcon, bool showLabel)
		{
			var button = new AppBarToggleButton
			{
				Width = double.NaN,
				MinWidth = showIcon ? 40 : 0,
				Label = cmd.ExtendedLabel,
				LabelPosition = showLabel ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed,
				IsChecked = cmd.IsOn,
				IsEnabled = cmd.IsExecutable,
			};

			if (showIcon)
			{
				if (!string.IsNullOrEmpty(cmd.Glyph.ThemedIconStyle))
					button.Content = cmd.Glyph.ToIcon();

				// The default AppBarToggleButton template presents Icon rather than Content,
				// so themed icons need the PathIcon fallback to be visible
				button.Icon = cmd.Glyph.ToOverflowIcon() ?? cmd.Glyph.ToFontIcon();
			}
			else
			{
				button.Loaded += CollapseIconViewbox;
			}

			ToolTipService.SetToolTip(button, cmd.HotKeyText is null ? cmd.ExtendedLabel : $"{cmd.ExtendedLabel} ({cmd.HotKeyText})");

			if (!string.IsNullOrEmpty(cmd.AccessKey))
			{
				button.AccessKey = cmd.AccessKey;
				button.AccessKeyInvoked += AppBarButton_AccessKeyInvoked;
			}

			if (!string.IsNullOrEmpty(cmd.AutomationId))
				AutomationProperties.SetAutomationId(button, cmd.AutomationId);

			if (cmd.HotKeyText is { } hotKeyText)
				button.KeyboardAcceleratorTextOverride = hotKeyText;

			button.Click += async (sender, _) =>
			{
				await cmd.ExecuteAsync();

				// Executing may leave the state unchanged (e.g. re-invoking the active sort option),
				// in which case no IsOn notification arrives to undo the control's automatic toggle
				((AppBarToggleButton)sender).IsChecked = cmd.IsOn;
			};

			PropertyChangedEventHandler commandPropertyChanged = (_, e) =>
			{
				if (e.PropertyName is nameof(IRichCommand.IsOn))
					button.IsChecked = cmd.IsOn;
				else if (e.PropertyName is nameof(IRichCommand.IsExecutable))
					button.IsEnabled = cmd.IsExecutable;
			};
			cmd.PropertyChanged += commandPropertyChanged;
			toggleButtonDetachActions.Add(() => cmd.PropertyChanged -= commandPropertyChanged);

			return button;
		}

		private void DetachToggleButtons()
		{
			foreach (var detach in toggleButtonDetachActions)
				detach();

			toggleButtonDetachActions.Clear();
		}

		internal static void ApplyIcon(AppBarButton button, RichGlyph glyph, bool setContent)
		{
			if (setContent)
				button.Content = glyph.ToIcon();

			button.Icon = glyph.ToFontIcon() ?? glyph.ToOverflowIcon();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(Viewbox))]
		internal static void CollapseIconViewbox(object sender, RoutedEventArgs e)
		{
			var button = (FrameworkElement)sender;
			button.Loaded -= CollapseIconViewbox;

			if (button.FindDescendant("ContentViewbox") is Viewbox vb)
				vb.Visibility = Visibility.Collapsed;
		}

		[DynamicWindowsRuntimeCast(typeof(AppBarSeparator))]
		internal static void UpdateCommandBarSeparatorVisibility(IList<ICommandBarElement> commands)
		{
			bool prevSep = true;
			AppBarSeparator? last = null;

			for (int i = 0; i < commands.Count; i++)
			{
				if (commands[i] is AppBarSeparator sep)
				{
					sep.Visibility = prevSep ? Visibility.Collapsed : Visibility.Visible;
					if (!prevSep) last = sep;
					prevSep = true;
				}
				else if (commands[i] is UIElement { Visibility: Visibility.Visible })
					prevSep = false;
			}

			if (last is not null && prevSep)
				last.Visibility = Visibility.Collapsed;
		}

		private void AttachContextFlyout(FrameworkElement button, string contextId, ToolbarItemSettingsEntry entry, int index)
		{
			var customize = new MenuFlyoutItem { Text = Strings.CustomizeToolbar.GetLocalizedResource() };
			customize.Click += (_, _) => Commands.CustomizeToolbar.Execute(null);
			var unpin = new MenuFlyoutItem { Text = Strings.Unpin.GetLocalizedResource() };
			unpin.Click += (_, _) => UnpinToolbarItem(contextId, index, entry);
			button.ContextFlyout = new MenuFlyout { Items = { customize, unpin } };
		}

		private void UnpinToolbarItem(string contextId, int index, ToolbarItemSettingsEntry entry)
		{
			var items = ToolbarDefaultsTemplate.ResolveToolbarItemsByContext(UserSettingsService.AppearanceSettingsService);
			if (!items.TryGetValue(contextId, out var list) || (uint)index >= (uint)list.Count)
				return;

			var target = list[index];
			if ((!string.IsNullOrEmpty(entry.CommandCode) && entry.CommandCode == target.CommandCode)
				|| (!string.IsNullOrEmpty(entry.CommandGroup) && entry.CommandGroup == target.CommandGroup))
			{
				list.RemoveAt(index);
				UserSettingsService.AppearanceSettingsService.CustomToolbarItems = items;
			}
		}

		private async Task PopulateGroupFlyoutAsync(MenuFlyout flyout, CommandGroup group)
		{
			flyout.Items.Clear();

			if (group is OpenWithCommandGroup)
			{
				await PopulateOpenWithFlyoutAsync(flyout);
				return;
			}

			foreach (var code in group.Commands)
				if (Commands[code] is { Code: not CommandCodes.None } cmd)
					flyout.Items.Add(CreateGroupMenuItem(cmd));

			if (group is NewItemCommandGroup && ViewModel?.InstanceViewModel.CanCreateFileInPage == true
				&& addItemService.GetEntries() is { Count: > 0 } entries)
			{
				flyout.Items.Add(new MenuFlyoutSeparator());
				var fmt = $"D{entries.Count.ToString().Length}";
				for (int i = 0; i < entries.Count; i++)
				{
					var item = await CreateShellNewEntryMenuItemAsync(entries[i]);
					item.AccessKey = (i + 1).ToString(fmt);
					item.Command = ViewModel!.CreateNewFileCommand;
					item.CommandParameter = entries[i];
					flyout.Items.Add(item);
				}
			}
		}

		private async Task PopulateOpenWithFlyoutAsync(MenuFlyout flyout)
		{
			var requestId = ++openWithFlyoutRequestId;

			flyout.Items.Add(new MenuFlyoutItem
			{
				Text = Strings.Loading.GetLocalizedResource(),
				IsEnabled = false,
			});

			openWithMenu?.Dispose();
			openWithMenu = null;

			OpenWithMenu? loadedOpenWithMenu = null;
			if (PageContext.SelectedItems.Count is 1 && PageContext.SelectedItem?.ItemPath is string path)
				loadedOpenWithMenu = await OpenWithMenu.GetForFileAsync(path);

			if (requestId != openWithFlyoutRequestId)
			{
				loadedOpenWithMenu?.Dispose();
				return;
			}

			openWithMenu = loadedOpenWithMenu;

			flyout.Items.Clear();

			if (openWithMenu is not null)
			{
				foreach (var item in openWithMenu.Items.Where(x => x.Type is MENU_ITEM_TYPE.MFT_STRING && !string.IsNullOrWhiteSpace(x.Label)))
					flyout.Items.Add(await CreateOpenWithMenuItemAsync(openWithMenu, item));
			}

			if (flyout.Items.Count == 0)
				flyout.Items.Add(CreateChooseAnotherAppMenuItem());
		}

		private static async Task<MenuFlyoutItem> CreateOpenWithMenuItemAsync(OpenWithMenu menu, Win32ContextMenuItem entry)
		{
			MenuFlyoutItem item;
			if (entry.Icon is { Length: > 0 })
			{
				using var ms = new MemoryStream(entry.Icon);
				var image = new BitmapImage();
				await image.SetSourceAsync(ms.AsRandomAccessStream());
				item = new MenuFlyoutItemWithImage { BitmapIcon = image };
			}
			else
			{
				item = new MenuFlyoutItem();
			}

			item.Text = entry.Label;
			item.Command = new AsyncRelayCommand(async () => await menu.InvokeItem(entry.ID));

			return item;
		}

		private MenuFlyoutItem CreateChooseAnotherAppMenuItem()
		{
			var cmd = Commands[CommandCodes.OpenItemWithApplicationPicker];

			var item = new MenuFlyoutItem
			{
				Text = Strings.ChooseAnotherApp.GetLocalizedResource(),
				Command = cmd,
				Visibility = cmd.IsExecutable ? Visibility.Visible : Visibility.Collapsed,
			};

			if (!string.IsNullOrEmpty(cmd.AutomationId))
				AutomationProperties.SetAutomationId(item, cmd.AutomationId);

			return item;
		}

		private static async Task<MenuFlyoutItem> CreateShellNewEntryMenuItemAsync(ShellNewEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.IconBase64))
			{
				using var ms = new MemoryStream(Convert.FromBase64String(entry.IconBase64));
				var image = new BitmapImage();
				await image.SetSourceAsync(ms.AsRandomAccessStream());
				return new MenuFlyoutItemWithImage { Text = entry.Name, BitmapIcon = image };
			}
			return new MenuFlyoutItem { Text = entry.Name, Icon = new FontIcon { Glyph = "\xE7C3" } };
		}

		private static MenuFlyoutItem CreateGroupMenuItem(IRichCommand cmd)
		{
			var item = new MenuFlyoutItem
			{
				Text = cmd.Label,
				Command = cmd,
				Visibility = cmd.IsExecutable ? Visibility.Visible : Visibility.Collapsed,
				Icon = cmd.Glyph.ToOverflowIcon() ?? cmd.Glyph.ToFontIcon(),
			};
			if (!string.IsNullOrWhiteSpace(cmd.AccessKey)) item.AccessKey = cmd.AccessKey;
			if (cmd.HotKeyText is string hk) item.KeyboardAcceleratorTextOverride = hk;
			if (!string.IsNullOrEmpty(cmd.AutomationId)) AutomationProperties.SetAutomationId(item, cmd.AutomationId);
			return item;
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
		private void SortGroup_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (sender is not MenuFlyoutSubItem menu) return;
			var items = menu.Items.TakeWhile(i => i is not MenuFlyoutSeparator).Where(i => i.IsEnabled).ToList();
			var fmt = $"D{items.Count.ToString().Length}";
			for (int i = 0; i < items.Count; i++)
				items[i].AccessKey = (i + 1).ToString(fmt);
		}

		private void AppBarButton_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
		}

		private void LayoutButton_Click(object sender, RoutedEventArgs e)
			=> LayoutFlyout?.Hide();

		private void CustomizeToolbar_Click(object sender, RoutedEventArgs e)
			=> Commands.CustomizeToolbar.Execute(null);
	}
}
