// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;
using Files.App.UserControls.Menus;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using WinRT;

namespace Files.App.Helpers.ContextFlyouts
{
	/// <summary>
	/// Lightweight MenuFlyout-based context menu host. Renders commands as regular menu items plus a horizontal
	/// icon-button row for the primary commands. Owns the open-direction handling: placement is resolved before
	/// the first render so the primary row sits next to the pointer (top when opening downward, bottom when
	/// opening upward) without visibly moving after the menu appears.
	/// </summary>
	public sealed class FastContextFlyout
	{
		private static ControlTemplate? primaryRowTemplate;
		private static IUserSettingsService? userSettingsService;

		private MenuFlyoutItem? primaryRow;
		private MenuFlyoutSeparator? primarySeparator;
		private List<ContextMenuFlyoutItemViewModel>? primaryModels;
		private bool openedUp;
		private bool placementResolved;
		private double estimatedWidth;
		private FrameworkElement? invocationAnchor;
		private Point? invocationPosition;

		// Supplies the live invocation point for framework-shown menus, whose ContextRequested can arrive after the
		// pre-render guess; pulled in Flyout_Opened so the direction correction uses the finger, not the stale cursor.
		public Func<(FrameworkElement Anchor, Point Position)?>? InvocationPointProvider { get; set; }

		public MenuFlyout Flyout { get; } = new()
		{
			Placement = FlyoutPlacementMode.Right,
			AreOpenCloseAnimationsEnabled = false,
		};

		public IList<MenuFlyoutItemBase> Items
			=> Flyout.Items;

		public FastContextFlyout()
		{
			Flyout.Opening += (sender, e) => App.LastOpenedFlyout = Flyout;
			Flyout.Opened += Flyout_Opened;
			Flyout.Closed += Flyout_Closed;
		}

		/// <summary>
		/// Clears the menu and the per-open placement state. Call at the start of every (re)build.
		/// </summary>
		public void Reset()
		{
			Flyout.Items.Clear();
			primaryRow = null;
			primarySeparator = null;
			primaryModels = null;
			openedUp = false;
			placementResolved = false;
			invocationAnchor = null;
			invocationPosition = null;
		}

		/// <summary>
		/// Builds the given models into the menu: the primary commands as an icon-button row on top, the rest as
		/// regular menu items. The overflow placeholder models ("ItemOverflow"/"OverflowSeparator") are markers
		/// only and are skipped; shell loaders append a real "Show more options" submenu instead.
		/// </summary>
		public void Build(List<ContextMenuFlyoutItemViewModel> models)
		{
			Reset();

			var primary = models.Where(x => x.IsPrimary && !x.IsHidden && x.ShowItem).ToList();
			var secondary = models
				.Where(x => !x.IsPrimary && x.ID != "ItemOverflow" && (x.Tag as string) is not ("ItemOverflow" or "OverflowSeparator"))
				.ToList();

			if (primary.Count > 0)
				SetPrimaryModels(primary);

			ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(secondary)?.ForEach(Flyout.Items.Add);

			ApplyEstimatedWidth(secondary, primary.Count);
		}

		/// <summary>
		/// Adds the primary-command icon-button row (and its separator) at the top of the menu.
		/// </summary>
		public void SetPrimaryModels(List<ContextMenuFlyoutItemViewModel> models)
		{
			primaryModels = models;
			primaryRow = BuildPrimaryCommandRow(models);
			primarySeparator = new MenuFlyoutSeparator();
			Flyout.Items.Insert(0, primaryRow);
			Flyout.Items.Insert(1, primarySeparator);
		}

		/// <summary>
		/// Appends a separator unless the menu is empty or already ends with one; returns the added separator.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
		public MenuFlyoutSeparator? AddSeparatorIfNeeded()
		{
			if (Flyout.Items.Count == 0 || Flyout.Items[^1] is MenuFlyoutSeparator)
				return null;

			var separator = new MenuFlyoutSeparator();
			Flyout.Items.Add(separator);
			return separator;
		}

		/// <summary>
		/// Appends a styled "Show more options" submenu (with a leading separator when needed). Add it before the
		/// async shell fetch so filling it later never resizes the main menu; drop it via
		/// <see cref="RemoveIfEmpty"/> when the shell turns out to have nothing to offer.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(Style))]
		private (MenuFlyoutSubItem SubMenu, MenuFlyoutSeparator? Separator) AddShowMoreOptionsSubMenu()
		{
			var separator = AddSeparatorIfNeeded();
			var subMenu = new MenuFlyoutSubItem
			{
				Text = Strings.ShowMoreOptions.GetLocalizedResource(),
				Icon = new FontIcon { Glyph = ((char)0xE712).ToString() },
				Style = App.Current.Resources["MenuFlyoutSubItemWithThemedIconStyle"] as Style,
			};
			Flyout.Items.Add(subMenu);

			return (subMenu, separator);
		}

		public void RemoveIfEmpty(MenuFlyoutSubItem subMenu, MenuFlyoutSeparator? separator)
		{
			if (subMenu.Items.Count != 0)
				return;

			Flyout.Items.Remove(subMenu);
			if (separator is not null)
				Flyout.Items.Remove(separator);
		}

		/// <summary>
		/// Pre-adds "Show more options" filled with the built-in overflow commands from the model list, when the
		/// user setting routes shell extensions to a submenu. Returns nulls when extensions render inline instead.
		/// </summary>
		public (MenuFlyoutSubItem? SubMenu, MenuFlyoutSeparator? Separator) AddShowMoreOptionsIfEnabled(List<ContextMenuFlyoutItemViewModel>? items = null)
		{
			userSettingsService ??= Ioc.Default.GetRequiredService<IUserSettingsService>();
			if (!userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				return (null, null);

			var (subMenu, separator) = AddShowMoreOptionsSubMenu();
			var overflowModel = items?.FirstOrDefault(x => x.ID == "ItemOverflow");
			if (overflowModel?.Items is { Count: > 0 } overflowItems)
				ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(overflowItems)?.ForEach(subMenu.Items.Add);

			return (subMenu, separator);
		}

		/// <summary>
		/// Collapses separator runs (and leading/trailing separators) left by pulling entries out of a shell list.
		/// </summary>
		public static void TrimSeparators(List<ContextMenuFlyoutItemViewModel> models)
		{
			for (var i = models.Count - 1; i > 0; i--)
			{
				if (models[i].ItemType is ContextMenuFlyoutItemType.Separator && models[i - 1].ItemType is ContextMenuFlyoutItemType.Separator)
					models.RemoveAt(i);
			}
			while (models.LastOrDefault()?.ItemType is ContextMenuFlyoutItemType.Separator)
				models.RemoveAt(models.Count - 1);
			while (models.FirstOrDefault()?.ItemType is ContextMenuFlyoutItemType.Separator)
				models.RemoveAt(0);
		}

		/// <summary>
		/// Inserts an element into the main menu just above the overflow area ("Show more options" and its
		/// separator), or at the end when there is none.
		/// </summary>
		public void InsertBeforeOverflow(MenuFlyoutItemBase element, MenuFlyoutSubItem? overflowSubMenu, MenuFlyoutSeparator? overflowSeparator)
			=> Items.Insert(GetOverflowInsertIndex(overflowSubMenu, overflowSeparator), element);

		private int GetOverflowInsertIndex(MenuFlyoutSubItem? overflowSubMenu, MenuFlyoutSeparator? overflowSeparator)
		{
			var index = overflowSeparator is not null ? Items.IndexOf(overflowSeparator) : -1;
			if (index < 0 && overflowSubMenu is not null)
				index = Items.IndexOf(overflowSubMenu);
			return index < 0 ? Items.Count : index;
		}

		/// <summary>
		/// BitLocker: the Turn on / Manage placeholders are only markers - collapses them both and inserts
		/// whichever entries the shell actually offers (only one applies to a drive) above the overflow area.
		/// The used models are removed from the given list.
		/// </summary>
		public void ApplyBitLockerModels(List<ContextMenuFlyoutItemViewModel> shellMenuItems, MenuFlyoutSubItem? overflowSubMenu, MenuFlyoutSeparator? overflowSeparator)
		{
			if (FindByTag("TurnOnBitLockerPlaceholder") is { } turnOnPlaceholder)
				turnOnPlaceholder.Visibility = Visibility.Collapsed;
			if (FindByTag("ManageBitLockerPlaceholder") is { } managePlaceholder)
				managePlaceholder.Visibility = Visibility.Collapsed;

			ContextMenuFlyoutItemViewModel?[] bitLockerModels =
			[
				shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem menuItem && (menuItem.CommandString?.StartsWith("encrypt-bde") ?? false)),
				shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "manage-bde" }),
			];

			foreach (var model in bitLockerModels)
			{
				if (model is null)
					continue;

				shellMenuItems.Remove(model);
				InsertBeforeOverflow(ContextFlyoutModelToElementHelper.GetMenuItem(model), overflowSubMenu, overflowSeparator);
			}
		}

		/// <summary>
		/// Adds the (already filtered) shell models: everything inline when there is no overflow submenu, the
		/// first 6 inline while shift is held, the rest inside "Show more options" - above its built-in commands
		/// when <paramref name="aboveExisting"/> is set, appended otherwise.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
		public void AddShellModels(List<ContextMenuFlyoutItemViewModel> models, bool shiftPressed, MenuFlyoutSubItem? overflowSubMenu, MenuFlyoutSeparator? overflowSeparator, bool aboveExisting = true)
		{
			List<ContextMenuFlyoutItemViewModel> mainModels = overflowSubMenu is null
				? models
				: shiftPressed ? models.Take(6).ToList() : [];
			var overflowModels = models.Skip(mainModels.Count).ToList();
			TrimSeparators(mainModels);
			TrimSeparators(overflowModels);

			if (mainModels.Count > 0)
			{
				var mainElements = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(mainModels);
				if (mainElements is { Count: > 0 })
				{
					var insertAt = GetOverflowInsertIndex(overflowSubMenu, overflowSeparator);
					if (insertAt > 0 && Items[insertAt - 1] is not MenuFlyoutSeparator)
						Items.Insert(insertAt++, new MenuFlyoutSeparator());
					foreach (var element in mainElements)
						Items.Insert(insertAt++, element);
				}
			}

			if (overflowSubMenu is null)
				return;

			var overflowElements = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(overflowModels);
			if (overflowElements is { Count: > 0 })
			{
				if (aboveExisting && overflowSubMenu.Items.Count > 0)
				{
					overflowSubMenu.Items.Insert(0, new MenuFlyoutSeparator());
					for (var i = overflowElements.Count - 1; i >= 0; i--)
						overflowSubMenu.Items.Insert(0, overflowElements[i]);
				}
				else
				{
					overflowElements.ForEach(overflowSubMenu.Items.Add);
				}
			}
			RemoveIfEmpty(overflowSubMenu, overflowSeparator);
		}

		public MenuFlyoutItemBase? FindByTag(string tag)
			=> Flyout.Items.FirstOrDefault(x => (x as FrameworkElement)?.Tag as string == tag);

		/// <summary>
		/// Replaces a placeholder item (looked up by Tag) with the given element at the same position.
		/// </summary>
		private bool SwapPlaceholder(string tag, MenuFlyoutItemBase replacement)
		{
			if (FindByTag(tag) is not { } placeholder)
				return false;

			var index = Flyout.Items.IndexOf(placeholder);
			if (index < 0)
				return false;

			Flyout.Items.RemoveAt(index);
			Flyout.Items.Insert(index, replacement);
			return true;
		}

		/// <summary>
		/// Collapses a leaf item and shows its submenu counterpart (both looked up by Tag), styling the submenu
		/// with the shared themed-icon template. Returns null unless both elements exist.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		public (MenuFlyoutItemBase Leaf, MenuFlyoutSubItem SubMenu)? SwapLeafForSubMenu(string leafTag, string subMenuTag, string? text, string? themedIconStyleKey)
		{
			if (FindByTag(leafTag) is not { } leaf || FindByTag(subMenuTag) is not MenuFlyoutSubItem subMenu)
				return null;

			if (text is not null)
				subMenu.Text = text;
			ApplyThemedSubMenuStyle(subMenu, themedIconStyleKey);

			leaf.Visibility = Visibility.Collapsed;
			subMenu.Visibility = Visibility.Visible;
			return (leaf, subMenu);
		}

		/// <summary>
		/// Applies the shared themed-icon submenu template; an empty icon slot is reserved when no icon style is
		/// given so the item's metrics match the others.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(Style))]
		private static void ApplyThemedSubMenuStyle(MenuFlyoutSubItem subMenu, string? themedIconStyleKey)
		{
			subMenu.Style = App.Current.Resources["MenuFlyoutSubItemWithThemedIconStyle"] as Style;
			if (themedIconStyleKey is not null && App.Current.Resources[themedIconStyleKey] is Style iconStyle)
				MenuFlyoutSubItemCustomProperties.SetThemedIconStyle(subMenu, iconStyle);
			else
				subMenu.Icon = new IconSourceElement();
		}

		/// <summary>
		/// Fills a swapped-in shell submenu once its model's sub-items load, or restores the leaf form when the
		/// shell offers no such entry (or it loads empty).
		/// </summary>
		public static void FillOrRevert((MenuFlyoutItemBase Leaf, MenuFlyoutSubItem SubMenu)? swap, ContextMenuFlyoutItemViewModel? model, Func<List<ContextMenuFlyoutItemViewModel>, List<ContextMenuFlyoutItemViewModel>?> getter)
		{
			if (swap is not { } target)
				return;

			void Revert()
			{
				target.SubMenu.Visibility = Visibility.Collapsed;
				target.Leaf.Visibility = Visibility.Visible;
			}

			if (model?.LoadSubMenuAction is not null)
				PopulateShellSubMenu(target.SubMenu, model, getter, Revert);
			else
				Revert();
		}

		/// <summary>
		/// Resolves the open direction and positions the primary row. Call after building, BEFORE the menu is shown.
		/// Callers that show the flyout themselves pass the anchor and anchor-relative point, since ShowAt (and thus
		/// <see cref="MenuFlyout.Target"/>) has not run yet.
		/// </summary>
		public void ResolvePlacement(FrameworkElement? anchor = null, Point? position = null)
		{
			invocationAnchor = anchor;
			invocationPosition = position;
			openedUp = PredictOpensUpward();
			placementResolved = true;
			FinalizePrimaryRowPosition();
		}

		/// <summary>
		/// Puts the primary row first (opened downward) or last (opened upward). Idempotent; also call after
		/// appending async content so the row is always the first or last item, never wedged before it.
		/// </summary>
		public void FinalizePrimaryRowPosition()
		{
			if (!placementResolved || primaryRow is null || primaryModels is not { Count: > 0 } models)
				return;

			var menuItems = Flyout.Items;
			var index = menuItems.IndexOf(primaryRow);
			if (index < 0 || index == (openedUp ? menuItems.Count - 1 : 0))
				return;

			// Rebuild a fresh row rather than move it: reparenting the custom-templated row drops its button panel.
			menuItems.Remove(primaryRow);
			if (primarySeparator is not null)
				menuItems.Remove(primarySeparator);
			primaryRow = BuildPrimaryCommandRow(models);

			if (openedUp)
			{
				primarySeparator = AddSeparatorIfNeeded();
				menuItems.Add(primaryRow);
			}
			else
			{
				primarySeparator = new MenuFlyoutSeparator();
				menuItems.Insert(0, primaryRow);
				menuItems.Insert(1, primarySeparator);
			}
		}

		/// <summary>
		/// Reserves the estimated content width on the primary row (which only exists in the main menu, so
		/// submenus keep their natural width). This prevents the shared keyboard-accelerator column from visibly
		/// widening the menu a frame after it opens.
		/// </summary>
		public void ApplyEstimatedWidth(IEnumerable<ContextMenuFlyoutItemViewModel> models, int primaryCount)
		{
			try
			{
				double maxContent = EstimateTextWidth(Strings.ShowMoreOptions.GetLocalizedResource());
				foreach (var m in models)
				{
					if (m.IsHidden || !m.ShowItem)
						continue;

					var w = EstimateTextWidth(m.Text);
					if (!string.IsNullOrEmpty(m.KeyboardAcceleratorTextOverride))
						w += EstimateTextWidth(m.KeyboardAcceleratorTextOverride) + 32;
					maxContent = Math.Max(maxContent, w);
				}

				// Icon column + item padding + chevron/shortcut allowance
				var contentWidth = maxContent + 104;
				var primaryWidth = primaryCount * 40 + 24;
				estimatedWidth = Math.Clamp(Math.Max(contentWidth, primaryWidth), 220.0, 460.0);

				if (primaryRow is not null)
					primaryRow.MinWidth = estimatedWidth;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Fills a shell submenu (Send to, Open with, ...) once its sub-items load, without blocking the menu-open
		/// path (the slow shell query runs in the background and the submenu contents fill in when ready). The
		/// getter (GetOpenWithItems / GetSendToItems) pulls the loaded sub-items out of the given shell model.
		/// When the shell yields no sub-items (e.g. no apps registered for the type) or the load fails,
		/// <paramref name="onEmpty"/> runs instead so the caller can revert to its leaf/placeholder form rather than
		/// leave an empty submenu.
		/// </summary>
		public static void PopulateShellSubMenu(MenuFlyoutSubItem subItem, ContextMenuFlyoutItemViewModel model, Func<List<ContextMenuFlyoutItemViewModel>, List<ContextMenuFlyoutItemViewModel>?> getter, Action? onEmpty = null)
		{
			_ = PopulateAsync();

			async Task PopulateAsync()
			{
				try
				{
					if (model.LoadSubMenuAction is not null)
						await model.LoadSubMenuAction();

					var items = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(getter([model]));
					if (items is { Count: > 0 })
					{
						subItem.Items.Clear();
						items.ForEach(subItem.Items.Add);
					}
					else
					{
						onEmpty?.Invoke();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
					// Revert the swapped-in empty submenu, same as the empty-result path, so a failed load doesn't strand it.
					onEmpty?.Invoke();
				}
			}
		}

		/// <summary>
		/// Converts a leaf placeholder item (Open with / Send to) into its final submenu form, keeping its Tag and
		/// position. Doing this BEFORE the menu is shown keeps item heights stable when the shell items land; the
		/// async loader then only fills the submenu contents. Returns the existing submenu when already converted.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		public MenuFlyoutSubItem? ConvertPlaceholderToSubMenu(string tag, string text, string? themedIconStyleKey)
		{
			if (FindByTag(tag) is not { } placeholder)
				return null;

			if (placeholder is MenuFlyoutSubItem existing)
				return existing;

			var subMenu = new MenuFlyoutSubItem
			{
				Text = text,
				Tag = tag,
			};
			ApplyThemedSubMenuStyle(subMenu, themedIconStyleKey);

			SwapPlaceholder(tag, subMenu);
			return subMenu;
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutPresenter))]
		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void Flyout_Opened(object? sender, object e)
		{
			try
			{
				if (Flyout.XamlRoot is null)
					return;

				Popup? popup = null;
				MenuFlyoutPresenter? presenter = null;
				foreach (var openPopup in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(Flyout.XamlRoot))
				{
					var candidate = openPopup.Child as MenuFlyoutPresenter ?? openPopup.Child?.FindDescendant<MenuFlyoutPresenter>();

					// Several menu popups can be open at once (a submenu, another pane's leftover menu); ours is
					// the one presenting this flyout's items
					if (candidate is not null && candidate.Items.Count > 0 && Flyout.Items.Count > 0 && ReferenceEquals(candidate.Items[0], Flyout.Items[0]))
					{
						presenter = candidate;
						popup = openPopup;
						break;
					}
				}
				if (presenter is null || popup is null)
					return;

				// Measure the shared accelerator-text column in the open frame; otherwise the menu visibly
				// widens a beat after it appears.
				presenter.UpdateLayout();

				// The presenter is a nameless container; keep it out of the tab order (the items are the
				// focus stops) so accessibility tooling doesn't flag a focusable element without a name.
				presenter.IsTabStop = false;

				// Cap the height to the visible area so late-loading shell extensions scroll instead of growing
				// the menu off-screen and repositioning it.
				if (presenter.XamlRoot.Content is FrameworkElement rootContent && rootContent.ActualHeight > 0)
					presenter.MaxHeight = rootContent.ActualHeight - 24;

				// Pull the touch/pointer point captured after the pre-render guess; the cursor is stale for touch.
				// Direct-position callers (widget/sidebar) leave the provider null.
				if (InvocationPointProvider?.Invoke() is { } invocation)
				{
					invocationAnchor = invocation.Anchor;
					invocationPosition = invocation.Position;
				}

				// The pre-render guess can be wrong within the estimation error at the flip boundary. Decide the
				// final position the way CommandBarFlyoutCommandBar does after its flyout is placed: from the
				// menu's ACTUAL position. A context menu is a windowed popup positioned by moving its own popup
				// window (the popup's offsets don't reflect the final placement), so the placed top edge comes
				// from the popup window's screen position; in-window popups fall back to the offset. The row goes
				// on whichever end is nearest the invocation point - also the best answer when the menu is taller
				// than the work area and the pointer lands mid-menu.
				var referenceScreenY = GetInvocationReferenceScreenY();
				if (!double.IsNaN(referenceScreenY) && presenter.ActualHeight > 0)
				{
					var popupScale = presenter.XamlRoot.RasterizationScale;
					double menuTopScreen;
					var mainWindowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(MainWindow.Instance.WindowHandle);
					if (presenter.XamlRoot.ContentIslandEnvironment?.AppWindowId is { } islandWindowId && islandWindowId.Value != mainWindowId.Value)
					{
						// The popup window is borderless, so its position is the menu's top-left
						menuTopScreen = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(islandWindowId).Position.Y;
					}
					else
					{
						menuTopScreen = ClientYToScreen(popup.VerticalOffset);
					}

					var menuBottomScreen = menuTopScreen + presenter.ActualHeight * popupScale;
					var actuallyOpenedUp = referenceScreenY - menuTopScreen > menuBottomScreen - referenceScreenY;
					if (actuallyOpenedUp != openedUp)
					{
						openedUp = actuallyOpenedUp;
						FinalizePrimaryRowPosition();
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Panel))]
		private void Flyout_Closed(object? sender, object e)
		{
			if (primaryRow?.Tag is not Panel row)
				return;

			foreach (var button in row.Children.OfType<Button>())
				button.KeyboardAccelerators.Clear();
		}

		/// <summary>
		/// Converts a Y coordinate in the main XamlRoot's space to physical screen coordinates through the
		/// window's client origin (AppWindow.Position is the OUTER top-left, off by the border/caption).
		/// </summary>
		private static double ClientYToScreen(double xamlRootY)
		{
			var scale = MainWindow.Instance.Content.XamlRoot?.RasterizationScale ?? 1.0;
			var clientOrigin = new System.Drawing.Point(0, 0);
			Windows.Win32.PInvoke.ClientToScreen(new(MainWindow.Instance.WindowHandle), ref clientOrigin);
			return clientOrigin.Y + xamlRootY * scale;
		}

		/// <summary>
		/// The invocation point in physical screen coordinates, most reliable first: the explicit invocation
		/// point when supplied, else the live mouse cursor, else the top of a small target. NaN when unknown.
		/// </summary>
		private double GetInvocationReferenceScreenY()
		{
			if (invocationAnchor is { } anchor && invocationPosition is { } position)
			{
				try
				{
					return ClientYToScreen(anchor.TransformToVisual(null).TransformPoint(position).Y);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			var cursorY = double.NaN;
			try
			{
				if (Windows.Win32.PInvoke.GetCursorPos(out var cursor))
					cursorY = cursor.Y;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			if (Flyout.Target is FrameworkElement target && target.ActualHeight > 0 && target.ActualHeight <= 300)
			{
				try
				{
					var scale = MainWindow.Instance.Content.XamlRoot?.RasterizationScale ?? 1.0;
					var targetTopScreen = ClientYToScreen(target.TransformToVisual(null).TransformPoint(new Point(0, 0)).Y);

					// Tolerance band around the target: a pointer invocation can land a few px outside the
					// row the flyout ends up anchored to
					var isCursorNearTarget = cursorY >= targetTopScreen - 24 && cursorY <= targetTopScreen + target.ActualHeight * scale + 24;
					return isCursorNearTarget ? cursorY : targetTopScreen;
				}
				catch
				{
					return double.NaN;
				}
			}

			return cursorY;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
		private bool PredictOpensUpward()
		{
			try
			{
				if (MainWindow.Instance.Content is not FrameworkElement content)
					return false;

				var scale = content.XamlRoot?.RasterizationScale ?? 1.0;
				var (referenceX, referenceY) = GetInvocationReferenceScreenPoint(content, scale);
				if (double.IsNaN(referenceY))
					return false;

				// Calibrated against realized MenuFlyoutPresenter heights (logical units)
				var estimatedHeight = 8.0;
				foreach (var it in Flyout.Items)
				{
					// Collapsed items (unswapped placeholders) take no space
					if (it.Visibility == Visibility.Collapsed)
						continue;

					estimatedHeight += it is MenuFlyoutSeparator ? 3.0 : ReferenceEquals(it, primaryRow) ? 44.0 : 34.0;
				}

				// The flyout is a windowed popup that can extend past the app window, so fit is decided
				// against the display's work area (screen minus taskbar), not the window. WinUI opens the
				// menu upward (bottom at the pointer, top clamped to the screen when needed) whenever it
				// does not fit below - it never slides down and never compares the two sides.
				var workArea = Microsoft.UI.Windowing.DisplayArea.GetFromPoint(
					new Windows.Graphics.PointInt32((int)referenceX, (int)referenceY),
					Microsoft.UI.Windowing.DisplayAreaFallback.Nearest).WorkArea;
				var spaceBelow = workArea.Y + workArea.Height - referenceY;
				return spaceBelow < estimatedHeight * scale;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		// The screen-physical point the menu opens from, most reliable first: the explicit invocation point when
		// supplied, else the live mouse cursor, else the top of a small target. NaN when unknown.
		private (double X, double Y) GetInvocationReferenceScreenPoint(FrameworkElement content, double scale)
		{
			// The exact invocation point when supplied - the caller's point (widget/sidebar) or the file-area
			// ContextRequested point. The cursor is stale for touch and keyboard, so it can't stand in for these.
			if (invocationAnchor is { } anchor && invocationPosition is { } position)
			{
				try
				{
					var windowPosition = MainWindow.Instance.AppWindow.Position;
					var pointInContent = anchor.TransformToVisual(content).TransformPoint(position);
					return (windowPosition.X + pointInContent.X * scale, windowPosition.Y + pointInContent.Y * scale);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			double cursorX = double.NaN, cursorY = double.NaN;
			try
			{
				Windows.Win32.PInvoke.GetCursorPos(out var cursor);
				cursorX = cursor.X;
				cursorY = cursor.Y;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			if (Flyout.Target is FrameworkElement target && target.ActualHeight > 0 && target.ActualHeight <= 300)
			{
				try
				{
					var windowPosition = MainWindow.Instance.AppWindow.Position;
					var targetTopLeft = target.TransformToVisual(content).TransformPoint(new Point(0, 0));
					var targetScreenX = windowPosition.X + targetTopLeft.X * scale;
					var targetScreenTop = windowPosition.Y + targetTopLeft.Y * scale;

					// Tolerance band around the target: a pointer invocation can land a few px outside the
					// row the flyout ends up anchored to
					var isCursorNearTarget = cursorY >= targetScreenTop - 24 && cursorY <= targetScreenTop + target.ActualHeight * scale + 24;
					return isCursorNearTarget ? (cursorX, cursorY) : (targetScreenX, targetScreenTop);
				}
				catch
				{
					return (double.NaN, double.NaN);
				}
			}

			return (cursorX, cursorY);
		}

		// Character-width estimate (no element creation/measure) - keeps menu-open work off the critical path.
		private static double EstimateTextWidth(string? text)
			=> (text?.Length ?? 0) * 7.3;

		[DynamicWindowsRuntimeCast(typeof(Style))]
		[DynamicWindowsRuntimeCast(typeof(ControlTemplate))]
		private MenuFlyoutItem BuildPrimaryCommandRow(List<ContextMenuFlyoutItemViewModel> models)
		{
			var row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 2,
				// Left/right arrows move between the icon buttons when one has keyboard focus
				XYFocusKeyboardNavigation = Microsoft.UI.Xaml.Input.XYFocusKeyboardNavigationMode.Enabled,
			};
			var buttonStyle = App.Current.Resources["PrimaryCommandButtonStyle"] as Style;
			foreach (var model in models)
			{
				var button = new Button
				{
					Style = buttonStyle,
					Command = model.Command,
					CommandParameter = model.CommandParameter,
					Content = model.ThemedIconModel.IsValid ? model.ThemedIconModel.ToThemedIcon() : null,
					IsEnabled = model.IsEnabled,
					AccessKey = model.AccessKey ?? string.Empty,
				};

				if (model.KeyboardAccelerator is { } accelerator)
				{
					// Fresh instance: the row is rebuilt on placement flips and an accelerator can have only one owner.
					button.KeyboardAccelerators.Add(new Microsoft.UI.Xaml.Input.KeyboardAccelerator
					{
						Key = accelerator.Key,
						Modifiers = accelerator.Modifiers,
					});
					button.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
				}

				Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, model.Text ?? string.Empty);
				Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(button, $"ContextMenuPrimaryButton_{model.Text}");
				ToolTipService.SetToolTip(button, model.Text);
				button.Click += (s, args) => Flyout.Hide();
				row.Children.Add(button);
			}

			primaryRowTemplate ??= (ControlTemplate)App.Current.Resources["PrimaryCommandMenuFlyoutItemTemplate"];

			// The row's min width sets the whole main menu's min width, reserving the measured content width
			// up-front so the accelerator column cannot widen it after it opens.
			var hostItem = new MenuFlyoutItem { Template = primaryRowTemplate, Tag = row, MinWidth = estimatedWidth };

			// The host draws nothing itself; keep it out of the accessibility tree so screen readers announce
			// the named icon buttons instead of an empty menu item.
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(hostItem, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);

			// Forward focus into the first enabled icon button so the row is keyboard-reachable, preserving
			// the focus state so only genuine keyboard navigation draws a focus rectangle.
			hostItem.GotFocus += (s, e) =>
			{
				if (ReferenceEquals(e.OriginalSource, hostItem) && row.Children.OfType<Button>().FirstOrDefault(b => b.IsEnabled) is { } firstButton)
					firstButton.Focus(hostItem.FocusState == FocusState.Keyboard ? FocusState.Keyboard : FocusState.Programmatic);
			};

			return hostItem;
		}
	}
}
