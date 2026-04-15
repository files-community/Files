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
using FlyoutPlacementMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode;

namespace Files.App.UserControls
{
	public sealed partial class Toolbar : UserControl
	{
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IModifiableCommandManager ModifiableCommands = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
		private bool isToolbarRefreshQueued;

		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel? ViewModel { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowViewControlButton { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowPreviewPaneButton { get; set; }

		public Toolbar()
		{
			InitializeComponent();
			Loaded += Toolbar_Loaded;
			Unloaded += Toolbar_Unloaded;
		}

		private void Toolbar_Loaded(object sender, RoutedEventArgs e)
		{
			foreach (var cmd in Commands) cmd.PropertyChanged += Command_PropertyChanged;
			PopulateToolbarItems();
			UserSettingsService.AppearanceSettingsService.PropertyChanged += AppearanceSettings_PropertyChanged;
		}

		private void Toolbar_Unloaded(object sender, RoutedEventArgs e)
		{
			foreach (var cmd in Commands) cmd.PropertyChanged -= Command_PropertyChanged;
			UserSettingsService.AppearanceSettingsService.PropertyChanged -= AppearanceSettings_PropertyChanged;
		}

		partial void OnViewModelChanged(NavigationToolbarViewModel? newValue)
		{
			if (newValue?.InstanceViewModel is not null)
			{
				newValue.InstanceViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
				PopulateToolbarItems();
			}
		}

		private void Command_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is not nameof(IRichCommand.IsExecutable) || sender is not IRichCommand cmd
				|| cmd.Code is CommandCodes.None || !cmd.IsAccessibleGlobally)
				return;
			// Debouncing in RequestToolbarRefresh() coalesces rapid IsExecutable changes,
			// so we don't need to check if the context visibility actually changed here.
			// Checking IsContextActive() loops through all commands and is expensive during
			// file selection when many commands' executability changes rapidly.
			RequestToolbarRefresh();
		}

		private void InstanceViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(CurrentInstanceViewModel.IsPageTypeNotHome)
				or nameof(CurrentInstanceViewModel.IsPageTypeRecycleBin))
				RequestToolbarRefresh();
		}

		private void AppearanceSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAppearanceSettingsService.CustomToolbarItems))
				RequestToolbarRefresh();
		}

		private void RequestToolbarRefresh()
		{
			if (isToolbarRefreshQueued) return;
			isToolbarRefreshQueued = true;
			DispatcherQueue.TryEnqueue(() =>
			{
				isToolbarRefreshQueued = false;
				PopulateToolbarItems();
			});
		}

		private void ContextCommandBar_Loaded(object sender, RoutedEventArgs e)
			=> PopulateToolbarItems();

		private void PopulateToolbarItems()
		{
			if (ContextCommandBar is null) return;
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
					if (CreateToolbarElement(entries[i]) is { } el)
					{
						if (el is AppBarButton btn && !ToolbarItemDescriptor.IsSeparatorCode(entries[i].CommandCode ?? ""))
							AttachContextFlyout(btn, contextId, entries[i], i);
						ContextCommandBar.PrimaryCommands.Add(el);
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

		private ICommandBarElement? CreateToolbarElement(ToolbarItemSettingsEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.CommandCode) && ToolbarItemDescriptor.IsSeparatorCode(entry.CommandCode))
				return new AppBarSeparator();
			var (showIcon, showLabel) = (entry.ShowIcon, entry.ShowLabel);
			if (!string.IsNullOrEmpty(entry.CommandGroup)
				&& Commands.Groups.All.FirstOrDefault(g => g.Name == entry.CommandGroup) is { } group)
			{
				showIcon &= !group.Glyph.IsNone;
				if (!showIcon && !showLabel) return null;
				var btn = CreateButton(showIcon, showLabel, group.DisplayName, group.DisplayName,
					group.AccessKey, group.AutomationId, group.Glyph);
				btn.Flyout = new MenuFlyout
				{
					Placement = group is NewItemCommandGroup
					? FlyoutPlacementMode.BottomEdgeAlignedLeft
					: FlyoutPlacementMode.Bottom
				};
				((MenuFlyout)btn.Flyout).Opening += (s, _) => PopulateGroupFlyout((MenuFlyout)s, group);
				btn.Style = (Style)Resources["ToolBarAppBarButtonFlyoutStyle"];
				if (showIcon) ApplyIcon(btn, group.Glyph, setContent: true);
				btn.IsEnabled = group.Commands.Any(c => c is not CommandCodes.None && Commands[c].IsExecutable);
				return btn;
			}
			if (!string.IsNullOrEmpty(entry.CommandCode) && Enum.TryParse<CommandCodes>(entry.CommandCode, out var code) && code != CommandCodes.None)
			{
				var mod = ModifiableCommands[code];
				var cmd = mod.Code != CommandCodes.None ? mod : Commands[code];
				showIcon &= !cmd.Glyph.IsNone;
				if (!showIcon && !showLabel) return null;
				var tooltip = cmd.HotKeyText is null ? cmd.ExtendedLabel : $"{cmd.ExtendedLabel} ({cmd.HotKeyText})";
				var btn = CreateButton(showIcon, showLabel, cmd.ExtendedLabel, tooltip,
					cmd.AccessKey, cmd.AutomationId, cmd.Glyph, cmd.HotKeyText);
				var useStyled = showIcon && !string.IsNullOrEmpty(cmd.Glyph.ThemedIconStyle);
				if (useStyled) btn.Style = (Style)Resources["ToolBarAppBarButtonFlyoutStyle"];
				if (showIcon) ApplyIcon(btn, cmd.Glyph, setContent: useStyled);
				btn.Command = cmd;
				return btn;
			}
			return null;
		}

		internal static ICommandBarElement? CreatePreviewElement(ToolbarItemDescriptor item, Style flyoutStyle)
		{
			if (item.IsSeparator) return new AppBarSeparator();
			var showIcon = item.ShowIcon && item.HasIcon;
			if (!showIcon && !item.ShowLabel) return null;
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
			if (showIcon) ApplyIcon(button, item.Glyph, setContent: useStyled);
			else button.Loaded += CollapseIconViewbox;
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
			if (!showIcon) button.Loaded += CollapseIconViewbox;
			ToolTipService.SetToolTip(button, tooltip);
			if (!string.IsNullOrEmpty(accessKey))
			{
				button.AccessKey = accessKey;
				button.AccessKeyInvoked += AppBarButton_AccessKeyInvoked;
			}
			if (!string.IsNullOrEmpty(automationId)) AutomationProperties.SetAutomationId(button, automationId);
			if (hotKeyText is not null) button.KeyboardAcceleratorTextOverride = hotKeyText;
			return button;
		}

		internal static void ApplyIcon(AppBarButton button, RichGlyph glyph, bool setContent)
		{
			if (setContent) button.Content = glyph.ToIcon();
			button.Icon = glyph.ToFontIcon() ?? glyph.ToOverflowIcon();
		}

		internal static void CollapseIconViewbox(object sender, RoutedEventArgs e)
		{
			var button = (AppBarButton)sender;
			button.Loaded -= CollapseIconViewbox;
			if (button.FindDescendant("ContentViewbox") is Viewbox vb) vb.Visibility = Visibility.Collapsed;
		}

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
			if (last is not null && prevSep) last.Visibility = Visibility.Collapsed;
		}

		private void AttachContextFlyout(AppBarButton button, string contextId, ToolbarItemSettingsEntry entry, int index)
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
			if (!items.TryGetValue(contextId, out var list) || (uint)index >= (uint)list.Count) return;
			var target = list[index];
			if ((!string.IsNullOrEmpty(entry.CommandCode) && entry.CommandCode == target.CommandCode)
				|| (!string.IsNullOrEmpty(entry.CommandGroup) && entry.CommandGroup == target.CommandGroup))
			{
				list.RemoveAt(index);
				UserSettingsService.AppearanceSettingsService.CustomToolbarItems = items;
				PopulateToolbarItems();
			}
		}

		private void PopulateGroupFlyout(MenuFlyout flyout, CommandGroup group)
		{
			flyout.Items.Clear();
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
					var item = CreateShellNewEntryMenuItem(entries[i]);
					item.AccessKey = (i + 1).ToString(fmt);
					item.Command = ViewModel!.CreateNewFileCommand;
					item.CommandParameter = entries[i];
					flyout.Items.Add(item);
				}
			}
		}

		private static MenuFlyoutItem CreateShellNewEntryMenuItem(ShellNewEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.IconBase64))
			{
				using var ms = new MemoryStream(Convert.FromBase64String(entry.IconBase64));
				var image = new BitmapImage();
				_ = image.SetSourceAsync(ms.AsRandomAccessStream());
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
				Icon = cmd.Glyph.ToFontIcon(),
			};
			if (!string.IsNullOrWhiteSpace(cmd.AccessKey)) item.AccessKey = cmd.AccessKey;
			if (cmd.HotKeyText is string hk) item.KeyboardAcceleratorTextOverride = hk;
			if (!string.IsNullOrEmpty(cmd.AutomationId)) AutomationProperties.SetAutomationId(item, cmd.AutomationId);
			return item;
		}

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
