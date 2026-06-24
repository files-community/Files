// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.ViewManagement;
using GridSplitter = Files.App.Controls.GridSplitter;

namespace Files.App.Views
{
	/// <summary>
	/// Represents <see cref="Page"/> that holds multiple panes.
	/// </summary>
	public sealed partial class ShellPanesPage : Page, IShellPanesPage, ITabBarItemContent, INotifyPropertyChanged
	{
		// Dependency injections

		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private AppModel AppModel { get; } = Ioc.Default.GetRequiredService<AppModel>();

		// Constants

		private const string ShellBorderFocusOnState = "ShellBorderFocusOnState";
		private const string ShellBorderFocusOffState = "ShellBorderFocusOffState";
		private const string ShellBorderDualPaneOffState = "ShellBorderDualPaneOffState";

		// Fields

		private bool _wasRightPaneVisible;
		private NavigationParams? _savedNavParamsRight;
		private readonly PointerEventHandler _panePointerPressedHandler;

		// Properties

		public bool IsLeftPaneActive
			=> ActivePane == (GetPane(0) as IShellPage);

		public bool IsRightPaneActive
			=> ActivePane == (GetPane(1) as IShellPage);

		public IFilesystemHelpers FilesystemHelpers
			=> ActivePane?.FilesystemHelpers!;

		public bool IsMultiPaneActive
			=> GetPaneCount() > 1;

		public bool IsMultiPaneAvailable
		{
			get
			{
				try
				{
					return !AppModel.IsMainWindowClosed && MainWindow.Instance.Bounds.Width > Constants.UI.MultiplePaneWidthThreshold;
				}
				catch
				{
					return false;
				}
			}
		}

		public IShellPage ActivePaneOrColumn
		{
			get
			{
				// Active shell is column view
				if (ActivePane is not null && ActivePane.IsColumnView && ActivePane.SlimContentPage is ColumnsLayoutPage columnLayoutPage)
					return columnLayoutPage.ActiveColumnShellPage;

				return ActivePane ?? GetPane(0)!;
			}
		}

		private ShellPaneArrangement _ShellPaneArrangement;
		public ShellPaneArrangement ShellPaneArrangement
		{
			get => _ShellPaneArrangement;
			set
			{
				if (_ShellPaneArrangement != value)
				{
					_ShellPaneArrangement = value;
					ArrangePanes();
					NotifyPropertyChanged(nameof(ShellPaneArrangement));
					Pane_ContentChanged(null, null!);
				}
			}
		}

		private bool _WindowIsCompact;
		public bool WindowIsCompact
		{
			get => _WindowIsCompact;
			set
			{
				if (value != _WindowIsCompact)
				{
					_WindowIsCompact = value;

					if (value)
					{
						// Close pane
						_wasRightPaneVisible = GetPaneCount() >= 2;

						if (_wasRightPaneVisible)
						{
							var currentPath = GetPane(1)?.TabBarItemParameter?.NavigationParameter as string ?? "Home";
							_savedNavParamsRight = new NavigationParams { NavPath = currentPath };
							RemovePane(1);
						}
					}
					else if (_wasRightPaneVisible)
					{
						// Add back pane
						if (GetPaneCount() == 1)
							AddPane();

						if (_savedNavParamsRight is not null)
						{
							NavParamsRight = _savedNavParamsRight;
							_savedNavParamsRight = null;
						}

						_wasRightPaneVisible = false;
					}

					NotifyPropertyChanged(nameof(IsMultiPaneAvailable));
				}
			}
		}

		private TabBarItemParameter? _TabBarItemParameter;
		public TabBarItemParameter? TabBarItemParameter
		{
			get => _TabBarItemParameter;
			set
			{
				if (_TabBarItemParameter != value)
				{
					_TabBarItemParameter = value;
					ContentChanged?.Invoke(this, value!);
				}
			}
		}

		private NavigationParams? _NavParamsLeft;
		public NavigationParams? NavParamsLeft
		{
			get => _NavParamsLeft;
			set
			{
				if (_NavParamsLeft != value)
				{
					_NavParamsLeft = value;
					NotifyPropertyChanged(nameof(NavParamsLeft));

					if (GetPane(0) is ModernShellPage page)
						page.NavParams = value!;
				}
			}
		}

		private NavigationParams? _NavParamsRight;
		public NavigationParams? NavParamsRight
		{
			get => _NavParamsRight;
			set
			{
				if (_NavParamsRight != value)
				{
					_NavParamsRight = value;
					NotifyPropertyChanged(nameof(NavParamsRight));

					if (GetPane(1) is ModernShellPage page)
						page.NavParams = value!;
				}
			}
		}

		private IShellPage? _ActivePane;
		public IShellPage? ActivePane
		{
			get => _ActivePane;
			set
			{
				if (_ActivePane != value)
				{
					_ActivePane = value;

					// Reset
					if (GetPane(0) is ModernShellPage firstShellPage)
						firstShellPage.IsCurrentInstance = false;
					if (GetPane(1) is ModernShellPage secondShellPage)
						secondShellPage.IsCurrentInstance = false;

					if (ActivePane is not null)
						ActivePane.IsCurrentInstance = IsCurrentInstance;

					NotifyPropertyChanged(nameof(ActivePane));
					NotifyPropertyChanged(nameof(IsLeftPaneActive));
					NotifyPropertyChanged(nameof(IsRightPaneActive));
					NotifyPropertyChanged(nameof(ActivePaneOrColumn));
					NotifyPropertyChanged(nameof(FilesystemHelpers));

					SetShadow();
				}
			}
		}

		private bool _IsCurrentInstance;
		public bool IsCurrentInstance
		{
			get => _IsCurrentInstance;
			set
			{
				if (_IsCurrentInstance == value)
					return;

				_IsCurrentInstance = value;

				// Reset
				if (GetPane(0) is ModernShellPage firstShellPage)
					firstShellPage.IsCurrentInstance = false;
				if (GetPane(1) is ModernShellPage secondShellPage)
					secondShellPage.IsCurrentInstance = false;

				if (ActivePane is not null)
				{
					ActivePane.IsCurrentInstance = value;

					if (value && ActivePane is BaseShellPage baseShellPage)
						baseShellPage.ContentPage?.ItemManipulationModel.FocusFileList();
				}

				CurrentInstanceChanged?.Invoke(null, this);
			}
		}


		// Events

		public static event EventHandler<ShellPanesPage>? CurrentInstanceChanged;
		public event EventHandler<TabBarItemParameter>? ContentChanged;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Constructor

		public ShellPanesPage()
		{
			_panePointerPressedHandler = Pane_PointerPressed;
			InitializeComponent();

			ShellPaneArrangement = GeneralSettingsService.ShellPaneArrangementOption;

			// Initialize the default pane
			AddPane();

			// Set default values
			ActivePane = GetPane(0);

			try
			{
				_WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
				MainWindow.Instance.SizeChanged += MainWindow_SizeChanged;
			}
			catch (Exception)
			{
				// Handle exception in case WinUI Windows is closed
				// (see https://github.com/files-community/Files/issues/15599)

				_WindowIsCompact = false;
			}

			// Open the secondary pane
			if (IsMultiPaneAvailable &&
				GeneralSettingsService.AlwaysOpenDualPaneInNewTab)
				AddPane();

			TabBar.TabDragStarted += TabBar_TabDragStarted;
			TabBar.TabDragCompleted += TabBar_TabDragCompleted;
		}

		// Public methods

		/// <inheritdoc/>
		public void OpenSecondaryPane(string path = "", ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (GetPaneCount() <= 1)
				AddPane(arrangement is ShellPaneArrangement.None ? GeneralSettingsService.ShellPaneArrangementOption : arrangement);

			NavParamsRight = new() { NavPath = string.IsNullOrEmpty(path) ? "Home" : path };
		}

		/// <inheritdoc/>
		public void OpenInOtherPane(string path)
		{
			if (!IsMultiPaneActive || string.IsNullOrEmpty(path))
				return;

			var otherPane = ActivePane == (IShellPage)GetPane(0)! ? GetPane(1) : GetPane(0);
			if (otherPane is null)
				return;

			otherPane.NavigateToPath(path);
			otherPane.Focus(FocusState.Programmatic);
		}

		/// <inheritdoc/>
		public void ArrangePanes(ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (arrangement is not ShellPaneArrangement.None)
				ShellPaneArrangement = arrangement;

			// Clear definitions
			RootGrid.RowDefinitions.Clear();
			RootGrid.ColumnDefinitions.Clear();

			if (ShellPaneArrangement == ShellPaneArrangement.Vertical)
			{
				foreach (var element in RootGrid.Children)
				{
					if (element is GridSplitter splitter)
					{
						RootGrid.ColumnDefinitions.Add(new() { Width = new(4) });
						splitter.Height = double.NaN;
						splitter.Width = 2;
					}
					else
					{
						RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 100d });
					}

					element.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
				}
			}
			else
			{
				foreach (var element in RootGrid.Children)
				{
					if (element is GridSplitter splitter)
					{
						RootGrid.RowDefinitions.Add(new() { Height = new(4) });
						splitter.Height = 2;
						splitter.Width = double.NaN;
					}
					else
					{
						RootGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 100d });
					}

					element.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);
				}
			}

			// Update the default cursor type on hover based on pane arrangement
			foreach (var sizer in GetSizers())
			{
				sizer?.ChangeCursor(
					InputSystemCursor.Create(
						ShellPaneArrangement is ShellPaneArrangement.Vertical
							? InputSystemCursorShape.SizeWestEast
							: InputSystemCursorShape.SizeNorthSouth));
			}
		}

		/// <inheritdoc/>
		public void CloseOtherPane()
		{
			if (!IsMultiPaneActive)
				return;

			if (ActivePane == (IShellPage)GetPane(0)!)
				RemovePane(1);
			else
				RemovePane(0);
		}

		/// <inheritdoc/>
		public void CloseActivePane()
		{
			if (ActivePane == (IShellPage)GetPane(0)!)
				RemovePane(0);
			else
				RemovePane(1);

			GetPane(0)?.Focus(FocusState.Programmatic);
			SetShadow();
		}

		/// <inheritdoc/>
		public void FocusOtherPane()
		{
			if (!IsMultiPaneActive)
				return;

			ActivePane = ActivePane == (IShellPage)GetPane(0)! ? GetPane(1) : GetPane(0);
			FocusActivePane();
		}

		/// <inheritdoc/>
		public void FocusActivePane()
		{
			if (ActivePane == (IShellPage)GetPane(0)!)
				GetPane(0)?.Focus(FocusState.Programmatic);
			else
				GetPane(1)?.Focus(FocusState.Programmatic);

			// Focus file list
			if (ActivePane is BaseShellPage baseShellPage)
				baseShellPage.ContentPage?.ItemManipulationModel.FocusFileList();
		}

		/// <inheritdoc/>
		public IEnumerable<ModernShellPage> GetPanes()
		{
			return RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 0).Cast<ModernShellPage>();
		}

		// Private methods

		private ModernShellPage? GetPane(int index = -1)
		{
			if (index is -1 || RootGrid.Children.Count - 1 < index)
				return null;

			var shellPage = RootGrid.Children[index * 2] as ModernShellPage;
			return shellPage;
		}

		private int GetPaneCount()
		{
			return (RootGrid.Children.Count + 1) / 2;
		}

		private IEnumerable<GridSplitter> GetSizers()
		{
			return RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 1).Cast<GridSplitter>();
		}

		private void AddPane(ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (arrangement is not ShellPaneArrangement.None)
				ShellPaneArrangement = arrangement;

			var currentPaneAlignmentDirection =
				RootGrid.ColumnDefinitions.Count is 0
					? RootGrid.RowDefinitions.Count is 0
						? ShellPaneArrangement
						: ShellPaneArrangement.Horizontal
					: ShellPaneArrangement.Vertical;

			// Adding new pane is not the first time
			if (RootGrid.Children.Count is not 0)
			{
				// Re-align shell pane
				ArrangePanes(arrangement);

				// Add sizer
				var sizer = new GridSplitter() { IsTabStop = false };
				sizer.DoubleTapped += Sizer_OnDoubleTapped;
				sizer.Loaded += Sizer_Loaded;
				sizer.ManipulationCompleted += Sizer_ManipulationCompleted;
				sizer.ManipulationStarted += Sizer_ManipulationStarted;

				// Add sizer
				RootGrid.Children.Add(sizer);

				// Set to a new column
				if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
				{
					RootGrid.ColumnDefinitions.Add(new() { Width = new(4) });
					sizer.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
					sizer.Height = double.NaN;
					sizer.Width = 2;
				}
				else
				{
					RootGrid.RowDefinitions.Add(new() { Height = new(4) });
					sizer.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);
					sizer.Height = 2;
					sizer.Width = double.NaN;
				}
			}

			// Add new pane
			var page = new ModernShellPage() { PaneHolder = this };
			RootGrid.Children.Add(page);

			if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
			{
				// Add a new definition
				RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 100d });
				page.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);

				// Reset width of every definition
				foreach (var definition in RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 0))
					definition.Width = new(1, GridUnitType.Star);
			}
			else
			{
				// Add a new definition
				RootGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 100d });
				page.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);

				// Reset width of every definition
				foreach (var definition in RootGrid.RowDefinitions.Where(x => RootGrid.RowDefinitions.IndexOf(x) % 2 == 0))
					definition.Height = new(1, GridUnitType.Star);
			}

			// Hook event
			page.ContentChanged += Pane_ContentChanged;
			page.Loaded += Pane_Loaded;

			// Focus
			ActivePane = GetPane(GetPaneCount() - 1);

			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private void RemovePane(int index = -1)
		{
			if (index is -1)
				return;

			// Get proper position of sizer that resides with the pane that is wanted to be removed
			var childIndex = index * 2 - 1;
			childIndex = childIndex >= 0 ? childIndex : 0;
			if (childIndex >= RootGrid.Children.Count)
				return;

			// Left pane is being removed
			if (childIndex == 0)
			{
				var wasMultiPaneActive = IsMultiPaneActive;

				// Remove sizer and pane
				RootGrid.Children.RemoveAt(0);

				if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
					RootGrid.ColumnDefinitions.RemoveAt(0);
				else
					RootGrid.RowDefinitions.RemoveAt(0);

				if (wasMultiPaneActive)
				{
					RootGrid.Children.RemoveAt(0);

					if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
						RootGrid.ColumnDefinitions.RemoveAt(0);
					else
						RootGrid.RowDefinitions.RemoveAt(0);

					RootGrid.Children[0].SetValue(Grid.ColumnProperty, 0);
					_NavParamsLeft = new() { NavPath = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? string.Empty };
					_NavParamsRight = null;
					ActivePane = GetPane(0);
				}
			}
			// Right pane is being removed
			else
			{
				// Remove sizer and pane
				RootGrid.Children.RemoveAt(childIndex);
				RootGrid.Children.RemoveAt(childIndex);

				if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
				{
					RootGrid.ColumnDefinitions.RemoveAt(childIndex);
					RootGrid.ColumnDefinitions.RemoveAt(childIndex);
				}
				else
				{
					RootGrid.RowDefinitions.RemoveAt(childIndex);
					RootGrid.RowDefinitions.RemoveAt(childIndex);
				}

				_NavParamsRight = null;
			}

			Pane_ContentChanged(null, null!);
			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private void SetShadow()
		{
			if (IsMultiPaneActive)
			{
				// Add theme shadow to the active pane
				if (GetPane(1) is ModernShellPage rightShellPage)
				{
					rightShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 0 : 32);
					VisualStateManager.GoToState(GetPane(0), IsLeftPaneActive ? ShellBorderFocusOnState : ShellBorderFocusOffState, true);
				}

				if (GetPane(0) is ModernShellPage leftShellPage)
				{
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 32 : 0);
					VisualStateManager.GoToState(GetPane(1), IsLeftPaneActive ? ShellBorderFocusOffState : ShellBorderFocusOnState, true);
				}
			}
			else
			{
				if (GetPane(0) is ModernShellPage leftShellPage)
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, 8);

				VisualStateManager.GoToState(GetPane(0), ShellBorderDualPaneOffState, true);
			}
		}

		// Override methods

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			if (eventArgs.Parameter is string navPath)
			{
				NavParamsLeft = new() { NavPath = navPath };
				NavParamsRight = new() { NavPath = "Home" };
			}
			else if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
			{
				NavParamsLeft = new()
				{
					NavPath = paneArgs.LeftPaneNavPathParam,
					SelectItem = paneArgs.LeftPaneSelectItemParam
				};

				// Creates new pane
				if (GetPaneCount() is 1 &&
					IsMultiPaneAvailable &&
					paneArgs.RightPaneNavPathParam is not null)
					AddPane();

				NavParamsRight = new()
				{
					NavPath = paneArgs.RightPaneNavPathParam,
					SelectItem = paneArgs.RightPaneSelectItemParam
				};

				ShellPaneArrangement =
					paneArgs.ShellPaneArrangement is ShellPaneArrangement.None
						? ShellPaneArrangement.Vertical
						: paneArgs.ShellPaneArrangement;
			}

			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = NavParamsLeft?.NavPath,
					LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
					RightPaneNavPathParam = GetPaneCount() >= 2 ? NavParamsRight?.NavPath : null,
					RightPaneSelectItemParam = GetPaneCount() >= 2 ? NavParamsRight?.SelectItem : null,
					ShellPaneArrangement = ShellPaneArrangement,
				}
			};
		}

		// Event methods

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;
		}

		private TabBarItem? _draggedTabItem;

		private void TabBar_TabDragStarted(object? sender, TabBarItem? draggedItem)
		{
			if (!IsCurrentInstance ||
				draggedItem is null ||
				(draggedItem.NavigationParameter?.NavigationParameter is PaneNavigationArguments p && !string.IsNullOrEmpty(p.RightPaneNavPathParam)) ||
				GetPaneCount() != 1 ||
				!IsMultiPaneAvailable)
				return;

			_draggedTabItem = draggedItem;
			TabDropOverlay.Visibility = Visibility.Visible;
		}

		private void TabBar_TabDragCompleted(object? sender, TabBarItem? draggedItem)
		{
			TabDropOverlay.Visibility = Visibility.Collapsed;
			HideDropIndicator();
			_indicatorInitialized = false;
			_draggedTabItem = null;
		}

		private SpriteVisual? _indicatorVisual;
		private RectangleClip? _indicatorClip;
		private bool _indicatorInitialized;

		private static readonly string[] _cornerProps =
			["TopLeftRadius", "TopRightRadius", "BottomRightRadius", "BottomLeftRadius"];

		private SpriteVisual GetOrCreateIndicatorVisual()
		{
			if (_indicatorVisual is not null)
				return _indicatorVisual;

			var compositor = ElementCompositionPreview.GetElementVisual(TabDropOverlay).Compositor;
			var accent = ((SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"]).Color;

			var sprite = compositor.CreateSpriteVisual();
			sprite.Brush = compositor.CreateColorBrush(accent);
			sprite.Opacity = 0f;
			sprite.AnchorPoint = new Vector2(0.5f, 0.5f);

			var clip = compositor.CreateRectangleClip();
			clip.StartAnimation("Right", BindToSize("X"));
			clip.StartAnimation("Bottom", BindToSize("Y"));
			sprite.Clip = clip;
			_indicatorClip = clip;

			if (new UISettings().AnimationsEnabled)
			{
				var anims = compositor.CreateImplicitAnimationCollection();
				anims["Offset"] = Tween(compositor.CreateVector3KeyFrameAnimation(), "Offset", 180);
				anims["Size"] = Tween(compositor.CreateVector2KeyFrameAnimation(), "Size", 180);
				anims["Opacity"] = Tween(compositor.CreateScalarKeyFrameAnimation(), "Opacity", 120);
				sprite.ImplicitAnimations = anims;

				var clipAnims = compositor.CreateImplicitAnimationCollection();
				foreach (var corner in _cornerProps)
					clipAnims[corner] = Tween(compositor.CreateVector2KeyFrameAnimation(), corner, 180);
				clip.ImplicitAnimations = clipAnims;
			}

			ElementCompositionPreview.SetElementChildVisual(TabDropOverlay, sprite);
			_indicatorVisual = sprite;
			return sprite;

			ExpressionAnimation BindToSize(string axis)
			{
				var anim = compositor.CreateExpressionAnimation($"v.Size.{axis}");
				anim.SetReferenceParameter("v", sprite);
				return anim;
			}

			static T Tween<T>(T anim, string target, int durationMs) where T : KeyFrameAnimation
			{
				anim.InsertExpressionKeyFrame(1f, "this.FinalValue");
				anim.Target = target;
				anim.Duration = TimeSpan.FromMilliseconds(durationMs);
				return anim;
			}
		}

		private void HideDropIndicator()
		{
			if (_indicatorVisual is not null)
				_indicatorVisual.Opacity = 0f;
		}

		private void ApplyZoneCorners(PaneDropZone zone)
		{
			if (_indicatorClip is null)
				return;

			var (tl, tr, br, bl) = zone switch
			{
				PaneDropZone.Left => (8f, 0f, 0f, 8f),
				PaneDropZone.Right => (0f, 8f, 8f, 0f),
				PaneDropZone.Top => (8f, 8f, 0f, 0f),
				_ => (0f, 0f, 8f, 8f),
			};
			_indicatorClip.TopLeftRadius = new Vector2(tl);
			_indicatorClip.TopRightRadius = new Vector2(tr);
			_indicatorClip.BottomRightRadius = new Vector2(br);
			_indicatorClip.BottomLeftRadius = new Vector2(bl);
		}

		private enum PaneDropZone { None, Left, Top, Right, Bottom }

		// Overlay diagonals carve it into four triangles; the cursor's triangle picks the edge to split toward.
		private PaneDropZone GetDropZone(DragEventArgs e)
		{
			var w = TabDropOverlay.ActualWidth;
			var h = TabDropOverlay.ActualHeight;
			if (w <= 0 || h <= 0)
				return PaneDropZone.None;

			var pos = e.GetPosition(TabDropOverlay);
			var nx = pos.X / w;
			var ny = pos.Y / h;

			if (ny < nx && ny < 1 - nx)
				return PaneDropZone.Top;
			if (ny > nx && ny > 1 - nx)
				return PaneDropZone.Bottom;
			return ny > nx ? PaneDropZone.Left : PaneDropZone.Right;
		}

		private void TabDropOverlay_DragOver(object sender, DragEventArgs e)
		{
			e.Handled = true;

			if (!e.DataView.Properties.ContainsKey(BaseTabBar.TabPathIdentifier))
			{
				HideDropIndicator();
				return;
			}

			var zone = GetDropZone(e);
			if (zone is PaneDropZone.None)
			{
				HideDropIndicator();
				e.AcceptedOperation = DataPackageOperation.None;
				return;
			}

			var w = (float)TabDropOverlay.ActualWidth;
			var h = (float)TabDropOverlay.ActualHeight;
			var vertical = zone is PaneDropZone.Left or PaneDropZone.Right;
			var size = vertical ? new Vector2(w / 2, h) : new Vector2(w, h / 2);
			var offset = zone switch
			{
				PaneDropZone.Left => new Vector3(w / 4, h / 2, 0),
				PaneDropZone.Right => new Vector3(3 * w / 4, h / 2, 0),
				PaneDropZone.Top => new Vector3(w / 2, h / 4, 0),
				_ => new Vector3(w / 2, 3 * h / 4, 0),
			};

			var visual = GetOrCreateIndicatorVisual();

			// Seed a zero-size state at the cursor with flat corners (no animation) so the assignment below grows out from there.
			if (!_indicatorInitialized)
			{
				var cursor = e.GetPosition(TabDropOverlay);
				var visualAnims = visual.ImplicitAnimations;
				var clipAnims = _indicatorClip!.ImplicitAnimations;
				visual.ImplicitAnimations = null;
				_indicatorClip.ImplicitAnimations = null;
				visual.Offset = new Vector3((float)cursor.X, (float)cursor.Y, 0);
				visual.Size = Vector2.Zero;
				_indicatorClip.TopLeftRadius = _indicatorClip.TopRightRadius =
					_indicatorClip.BottomRightRadius = _indicatorClip.BottomLeftRadius = Vector2.Zero;
				visual.ImplicitAnimations = visualAnims;
				_indicatorClip.ImplicitAnimations = clipAnims;
				_indicatorInitialized = true;
			}

			ApplyZoneCorners(zone);
			visual.Offset = offset;
			visual.Size = size;
			visual.Opacity = 0.35f;

			e.AcceptedOperation = DataPackageOperation.Move;
			e.DragUIOverride.Caption = (vertical
				? Strings.AddVerticalPaneDescription
				: Strings.SplitPaneHorizontallyDescription).GetLocalizedResource();
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.IsGlyphVisible = false;
		}

		private void TabDropOverlay_DragLeave(object sender, DragEventArgs e)
			=> HideDropIndicator();

		private void TabDropOverlay_Drop(object sender, DragEventArgs e)
		{
			HideDropIndicator();

			if (!e.DataView.Properties.TryGetValue(BaseTabBar.TabPathIdentifier, out var raw) ||
				raw is not string serialized)
				return;

			var zone = GetDropZone(e);
			if (zone is PaneDropZone.None)
				return;

			TabBarItemParameter tabArgs;
			try
			{
				tabArgs = TabBarItemParameter.Deserialize(serialized);
			}
			catch (JsonException)
			{
				return;
			}

			var draggedPath = (tabArgs.NavigationParameter as PaneNavigationArguments)?.LeftPaneNavPathParam
				?? tabArgs.NavigationParameter as string
				?? string.Empty;
			var nearSide = zone is PaneDropZone.Left or PaneDropZone.Top;
			var arrangement = zone is PaneDropZone.Left or PaneDropZone.Right
				? ShellPaneArrangement.Vertical
				: ShellPaneArrangement.Horizontal;
			var currentPath = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? "Home";

			OpenSecondaryPane(nearSide ? currentPath : draggedPath, arrangement);
			if (nearSide)
			{
				NavParamsLeft = new() { NavPath = string.IsNullOrEmpty(draggedPath) ? "Home" : draggedPath };
				// Override AddPane's focus on the new pane; the dropped content lives in pane 0 here.
				ActivePane = GetPane(0);
			}

			// Self-drop: leave the close-on-drop flag unset so the source tab survives the split.
			if (!ReferenceEquals(_draggedTabItem?.TabItemContent, this))
				ApplicationData.Current.LocalSettings.Values[BaseTabBar.TabDropHandledIdentifier] = true;
		}

		private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
		}

		private void Pane_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is UIElement element)
			{
				element.GotFocus += Pane_GotFocus;
				element.RightTapped += Pane_RightTapped;
				element.AddHandler(UIElement.PointerPressedEvent, _panePointerPressedHandler, true);
			}
		}

		private void Pane_ContentChanged(object? sender, TabBarItemParameter e)
		{
			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? e?.NavigationParameter as string,
					RightPaneNavPathParam = GetPaneCount() >= 2 ? GetPane(1)?.TabBarItemParameter?.NavigationParameter as string : null,
					ShellPaneArrangement = ShellPaneArrangement,
				}
			};
		}

		private void Pane_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Focus pane if interaction suggests intent to focus:
			// 1. Sender is not the currently active pane (user is switching panes), or the sender is the active pane,
			// but the user is refocusing the pane (e.g. user taps pane to refocus while the Omnibar flyout is open)
			// 2. AND the sender is a valid shell page not using a column-based layout
			if (((IsMultiPaneActive && sender != ActivePane) || e.Pointer.PointerDeviceType == PointerDeviceType.Touch) && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				(sender as UIElement)?.Focus(FocusState.Pointer);
		}

		private void Pane_GotFocus(object sender, RoutedEventArgs e)
		{
			var isLeftPane = sender == (GetPane(0) as IShellPage);

			// Clear selection in left pane
			if (isLeftPane && GetPane(1) is ModernShellPage secondShellPage && (secondShellPage.SlimContentPage?.IsItemSelected ?? false))
			{
				secondShellPage.SlimContentPage.LockPreviewPaneContent = true;
				secondShellPage.SlimContentPage.ItemManipulationModel.ClearSelection();
				secondShellPage.SlimContentPage.LockPreviewPaneContent = false;
			}
			// Clear selection in right pane
			else if (!isLeftPane && GetPane(0) is ModernShellPage firstShellPage && (firstShellPage.SlimContentPage?.IsItemSelected ?? false))
			{
				firstShellPage.SlimContentPage.LockPreviewPaneContent = true;
				firstShellPage.SlimContentPage.ItemManipulationModel.ClearSelection();
				firstShellPage.SlimContentPage.LockPreviewPaneContent = false;
			}

			var newActivePane = isLeftPane ? GetPane(0) : GetPane(1);
			if (ActivePane != (newActivePane as IShellPage))
				ActivePane = newActivePane;
		}

		private void Pane_RightTapped(object sender, RoutedEventArgs e)
		{
			if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				((UIElement)sender).Focus(FocusState.Programmatic);
		}

		private void Sizer_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is GridSplitter sizer)
			{
				sizer.ChangeCursor(
					InputSystemCursor.Create(
						ShellPaneArrangement is ShellPaneArrangement.Vertical
							? InputSystemCursorShape.SizeWestEast
							: InputSystemCursorShape.SizeNorthSouth));
			}
		}

		private void Sizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (ShellPaneArrangement is ShellPaneArrangement.Vertical)
			{
				var definitions = RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 0);
				definitions?.ForEach(x => x.Width = new GridLength(1, GridUnitType.Star));
			}
			else
			{
				var definitions = RootGrid.RowDefinitions.Where(x => RootGrid.RowDefinitions.IndexOf(x) % 2 == 0);
				definitions?.ForEach(x => x.Height = new GridLength(1, GridUnitType.Star));
			}
		}

		private void Sizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(
				InputSystemCursor.Create(
					ShellPaneArrangement is ShellPaneArrangement.Vertical
						? InputSystemCursorShape.SizeWestEast
						: InputSystemCursorShape.SizeNorthSouth));
		}

		private void Sizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (GetPane(1) is ModernShellPage secondShellPage &&
				secondShellPage.ActualWidth <= 100)
				RemovePane(1);

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Disposer

		public void Dispose()
		{
			App.Logger.LogInformation($"ShellPanesPage.Dispose: PaneCount={GetPaneCount()}, ActivePane={LogPathHelper.GetPathIdentifier(ActivePane?.TabBarItemParameter?.NavigationParameter?.ToString())}");

			TabBar.TabDragStarted -= TabBar_TabDragStarted;
			TabBar.TabDragCompleted -= TabBar_TabDragCompleted;

			MainWindow.Instance.SizeChanged -= MainWindow_SizeChanged;

			// Dispose panes
			foreach (var pane in GetPanes())
			{
				pane.Loaded -= Pane_Loaded;
				pane.ContentChanged -= Pane_ContentChanged;
				pane.GotFocus -= Pane_GotFocus;
				pane.RightTapped -= Pane_RightTapped;
				pane.RemoveHandler(UIElement.PointerPressedEvent, _panePointerPressedHandler);
				pane.Dispose();
			}

			// Dispose sizers
			foreach (var sizer in GetSizers())
			{
				sizer.DoubleTapped -= Sizer_OnDoubleTapped;
				sizer.Loaded -= Sizer_Loaded;
				sizer.ManipulationCompleted -= Sizer_ManipulationCompleted;
				sizer.ManipulationStarted -= Sizer_ManipulationStarted;
			}
		}
	}
}
