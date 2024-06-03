// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;

namespace Files.App.Views
{
	/// <summary>
	/// Represents <see cref="Page"/> that holds multiple panes.
	/// </summary>
	public sealed partial class ShellPanesPage : Page, IShellPanesPage, ITabBarItemContent
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private bool _wasRightPaneVisible;

		// Properties

		private StatusBar StatusBar
			=> ((Frame)MainWindow.Instance.Content).FindDescendant<StatusBar>()!;

		public bool IsLeftPaneActive
			=> ActivePane == (GetPane(0) as IShellPage);

		public bool IsRightPaneActive
			=> ActivePane == (GetPane(1) as IShellPage);

		public IFilesystemHelpers FilesystemHelpers
			=> ActivePane?.FilesystemHelpers!;

		public bool IsMultiPaneActive
			=> GetPaneCount() > 1;

		public bool IsMultiPaneEnabled
			=> App.AppModel.IsMainWindowClosed ? false : MainWindow.Instance.Bounds.Width > Constants.UI.MultiplePaneWidthThreshold;

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
						// Collapse right pane
						_wasRightPaneVisible = IsRightPaneVisible;
						IsRightPaneVisible = false;
					}
					else if (_wasRightPaneVisible)
					{
						// Show right pane if window gets larger
						IsRightPaneVisible = true;
						_wasRightPaneVisible = false;
					}

					NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
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

		private bool _IsRightPaneVisible;
		public bool IsRightPaneVisible
		{
			get => _IsRightPaneVisible;
			set
			{
				if (value != _IsRightPaneVisible)
				{
					_IsRightPaneVisible = value;
					if (!_IsRightPaneVisible)
					{
						ActivePane = GetPane(0);
						Pane_ContentChanged(null!, null!);
					}

					NotifyPropertyChanged(nameof(IsRightPaneVisible));
					NotifyPropertyChanged(nameof(IsMultiPaneActive));

					if (value)
					{
						AddPane();

						RootGrid.ColumnDefinitions[2].MinWidth = 100;
						RootGrid.ColumnDefinitions[2].Width = new(1, GridUnitType.Star);
						RootGrid.ColumnDefinitions[0].Width = new(1, GridUnitType.Star);
					}
					else
					{
						RemovePane((RootGrid.Children.Count - 1) / 2);

						//RootGrid.ColumnDefinitions[2].MinWidth = 0;
						//RootGrid.ColumnDefinitions[2].Width = new(0);
						RootGrid.ColumnDefinitions[0].Width = new(1, GridUnitType.Star);
					}
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
			InitializeComponent();

			// Initialize the default pane
			AddPane();

			// Set default values
			ActivePane = GetPane(0);
			_WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
			IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;

			MainWindow.Instance.SizeChanged += MainWindow_SizeChanged;
		}

		// Public methods

		public ModernShellPage? GetPane(int index = -1)
		{
			if (index is -1 || RootGrid.Children.Count - 1 < index)
				return null;

			var shellPage = RootGrid.Children[index * 2] as ModernShellPage;
			return shellPage;
		}

		public int GetPaneCount()
		{
			return (RootGrid.Children.Count + 1) / 2;
		}

		public void AddPane()
		{
			// Adding new pane is not the first time
			if (RootGrid.Children.Count is not 0)
			{
				var sizer = new GridSplitter();
				sizer.DoubleTapped += PaneResizer_OnDoubleTapped;
				sizer.Loaded += PaneResizer_Loaded;
				sizer.ManipulationCompleted += PaneResizer_ManipulationCompleted;
				sizer.ManipulationStarted += PaneResizer_ManipulationStarted;

				// Add column definition for sizer
				RootGrid.ColumnDefinitions.Add(new());

				// Add sizer
				RootGrid.Children.Add(sizer);
				sizer.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);

				// TODO: Set binding to the sizer and previous pane
			}

			// Add column definition for new pane
			RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });

			// Add new pane
			var page = new ModernShellPage() { PaneHolder = this };
			RootGrid.Children.Add(page);
			page.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);

			// Hook event
			page.ContentChanged += Pane_ContentChanged;
			page.Loaded += Pane_Loaded;
		}

		public void RemovePane(int index = -1)
		{
			if (index is -1)
				return;

			// Get proper position of sizer that resides with the pane that is wanted to be removed
			var childIndex = index * 2 - 1;
			childIndex = childIndex >= 0 ? childIndex : 0;

			// Remove sizer and pane
			RootGrid.Children.RemoveAt(childIndex);
			RootGrid.Children.RemoveAt(childIndex);
		}

		public void OpenSecondaryPane(string path)
		{
			// Add right pane within this property's setter
			IsRightPaneVisible = true;

			NavParamsRight = new() { NavPath = path };
			ActivePane = GetPane(1);
		}

		public void CloseSecondaryPane()
		{
			// Remove right pane within this property's setter
			IsRightPaneVisible = false;

			GetPane(0)?.Focus(FocusState.Programmatic);
			SetShadow();
		}

		public void FocusLeftPane()
		{
			GetPane(0)?.Focus(FocusState.Programmatic);
		}

		public void FocusRightPane()
		{
			GetPane(1)?.Focus(FocusState.Programmatic);
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
				NavParamsRight = new()
				{
					NavPath = paneArgs.RightPaneNavPathParam,
					SelectItem = paneArgs.RightPaneSelectItemParam
				};

				IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam is not null;
			}

			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = NavParamsLeft?.NavPath,
					LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
					RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight?.NavPath : null,
					RightPaneSelectItemParam = IsRightPaneVisible ? NavParamsRight?.SelectItem : null,
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

		private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
		}

		private void Pane_ContentChanged(object? sender, TabBarItemParameter e)
		{
			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? e?.NavigationParameter as string,
					RightPaneNavPathParam = IsRightPaneVisible ? GetPane(1)?.TabBarItemParameter?.NavigationParameter as string : null
				}
			};
		}

		private void Pane_Loaded(object sender, RoutedEventArgs e)
		{
			((UIElement)sender).GotFocus += Pane_GotFocus;
			((UIElement)sender).RightTapped += Pane_RightTapped;
			((UIElement)sender).PointerPressed += Pane_PointerPressed;
		}

		private void Pane_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
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

		private void SetShadow()
		{
			if (IsMultiPaneActive)
			{
				// Add theme shadow to the active pane
				if (GetPane(1) is ModernShellPage rightShellPage)
				{
					rightShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 0 : 32);
					VisualStateManager.GoToState(GetPane(0), IsLeftPaneActive ? "ShellBorderFocusOnState" : "ShellBorderFocusOffState", true);
				}

				if (GetPane(0) is ModernShellPage leftShellPage)
				{
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 32 : 0);
					VisualStateManager.GoToState(GetPane(1), IsLeftPaneActive ? "ShellBorderFocusOffState" : "ShellBorderFocusOnState", true);
				}
			}
			else
			{
				if (GetPane(0) is ModernShellPage leftShellPage)
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, 8);

				VisualStateManager.GoToState(GetPane(0), "ShellBorderDualPaneOffState", true);
			}
		}

		private void Pane_RightTapped(object sender, RoutedEventArgs e)
		{
			if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				((UIElement)sender).Focus(FocusState.Programmatic);
		}

		private void PaneResizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var sizerColumns = RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 1);
			sizerColumns?.ForEach(x => x.Width = new GridLength(1, GridUnitType.Star));
		}

		private void PaneResizer_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is GridSplitter sizer)
				sizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void PaneResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void PaneResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (GetPane(1) is ModernShellPage secondShellPage && secondShellPage.ActualWidth <= 100)
				IsRightPaneVisible = false;

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Disposer

		public void Dispose()
		{
			MainWindow.Instance.SizeChanged -= MainWindow_SizeChanged;
			GetPane(0)?.Dispose();
			GetPane(1)?.Dispose();

			var sizerColumns = RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 1)?.Cast<GridSplitter>();
			if (sizerColumns is not null)
			{
				foreach (var item in sizerColumns)
					item.DoubleTapped -= PaneResizer_OnDoubleTapped;
			}
		}
	}
}
