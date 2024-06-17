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
		private AppModel AppModel { get; } = Ioc.Default.GetRequiredService<AppModel>();

		// Constants

		private const string ShellBorderFocusOnState = "ShellBorderFocusOnState";
		private const string ShellBorderFocusOffState = "ShellBorderFocusOffState";
		private const string ShellBorderDualPaneOffState = "ShellBorderDualPaneOffState";

		// Fields

		private bool _wasRightPaneVisible;

		// Properties

		public bool IsLeftPaneActive
			=> ActivePane == (GetPane(0) as IShellPage);

		public bool IsRightPaneActive
			=> ActivePane == (GetPane(1) as IShellPage);

		public IFilesystemHelpers FilesystemHelpers
			=> ActivePane?.FilesystemHelpers!;

		public bool IsMultiPaneActive
			=> GetPaneCount() > 1;

		public bool IsMultiPaneEnabled
			=> !AppModel.IsMainWindowClosed && MainWindow.Instance.Bounds.Width > Constants.UI.MultiplePaneWidthThreshold;

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
						// Show right pane back if window gets larger again
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

		private bool _IsLeftPaneVisible;
		public bool IsLeftPaneVisible
		{
			get => _IsLeftPaneVisible;
			set
			{
				if (value != _IsLeftPaneVisible)
				{
					_IsLeftPaneVisible = value;

					if (value)
					{
						AddPane();
					}
					else
					{
						ActivePane = GetPane(1);
						Pane_ContentChanged(null!, null!);
						RemovePane(0);
					}

					NotifyPropertyChanged(nameof(IsLeftPaneVisible));
					NotifyPropertyChanged(nameof(IsMultiPaneActive));

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

					if (value)
					{
						AddPane();
					}
					else
					{
						ActivePane = GetPane(0);
						Pane_ContentChanged(null!, null!);
						RemovePane(1);
					}

					NotifyPropertyChanged(nameof(IsRightPaneVisible));
					NotifyPropertyChanged(nameof(IsMultiPaneActive));

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

		public void OpenSecondaryPane(string path)
		{
			// Add right pane within this property's setter
			IsRightPaneVisible = true;

			NavParamsRight = new() { NavPath = path };
			ActivePane = GetPane(1);
		}

		public void CloseActivePane()
		{
			if (ActivePane == (GetPane(0) as IShellPage))
				IsLeftPaneVisible = false;
			else
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
			return RootGrid.Children.Count / 2;
		}

		private void AddPane()
		{
			if (RootGrid.Children.Count is 0)
			{
				// This is the first time to add pane
				var dummy = new Grid();
				RootGrid.ColumnDefinitions.Add(new() { Width = new(0) });
				RootGrid.Children.Add(dummy);
				dummy.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
			}
			else
			{
				var sizer = new GridSplitter() { IsTabStop = false };
				Canvas.SetZIndex(sizer, 150);
				sizer.DoubleTapped += Sizer_OnDoubleTapped;
				sizer.Loaded += Sizer_Loaded;
				sizer.ManipulationCompleted += Sizer_ManipulationCompleted;
				sizer.ManipulationStarted += Sizer_ManipulationStarted;

				// Add column definition for sizer
				RootGrid.ColumnDefinitions.Add(new() { Width = new(4) });

				// Add sizer
				RootGrid.Children.Add(sizer);
				sizer.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
			}

			// Add column definition for new pane
			RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 100d });

			// Add new pane
			var page = new ModernShellPage() { PaneHolder = this };
			RootGrid.Children.Add(page);
			page.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);

			// Hook event
			page.ContentChanged += Pane_ContentChanged;
			page.Loaded += Pane_Loaded;

			// Reset width of every column
			foreach (var column in RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 0))
				column.Width = new(1, GridUnitType.Star);

			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private void RemovePane(int index = -1)
		{
			if (index is -1)
				return;

			// Get proper position of sizer that resides with the pane that is wanted to be removed
			var childIndex = index * 2;

			if (childIndex + 1 >= RootGrid.Children.Count)
				return;

			// Remove sizer and pane
			RootGrid.Children.RemoveAt(childIndex);
			RootGrid.Children.RemoveAt(childIndex);
			RootGrid.ColumnDefinitions.RemoveAt(childIndex);
			RootGrid.ColumnDefinitions.RemoveAt(childIndex);

			// Get range of items to rearrange column index
			var range = RootGrid.Children.ToArray()[childIndex..];

			foreach (var child in range)
			{
				var columnIndex = (int)child.GetValue(Grid.ColumnProperty);
				child.SetValue(Grid.ColumnProperty, columnIndex - 2);
			}

			// NOTE: This is workaround to avoid major refactor of making the pane generation to be compatible to generate more than 2 panes
			if (index == 0)
			{
				NavParamsLeft = new() { NavPath = NavParamsRight?.NavPath ?? string.Empty };
				NavParamsRight = null;
				IsRightPaneVisible = false;
			}

			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private bool IsActivePane(ModernShellPage pane)
		{
			return ActivePane == (IShellPage)pane ? true : false;
		}

		private void SetShadow()
		{
			if (IsMultiPaneActive)
			{
				for (int index = 0; index < GetPaneCount(); index++)
				{
					if (GetPane(index) is ModernShellPage shellPage)
					{
						shellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsActivePane(shellPage) ? 32 : 0);
						VisualStateManager.GoToState(shellPage, IsLeftPaneActive ? ShellBorderFocusOnState : ShellBorderFocusOffState, true);
					}
				}
			}
			else
			{
				if (GetPane(0) is ModernShellPage shellPage)
				{
					shellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, 8);
					VisualStateManager.GoToState(shellPage, ShellBorderDualPaneOffState, true);
				}
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
				NavParamsLeft = new() { NavPath = paneArgs.LeftPaneNavPathParam };

				// Creates a secondary pane
				IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam is not null;

				NavParamsRight = new() { NavPath = paneArgs.RightPaneNavPathParam };
			}

			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = NavParamsLeft?.NavPath,
					RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight?.NavPath : null,
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
			if (sender is UIElement element)
			{
				element.GotFocus += Pane_GotFocus;
				element.RightTapped += Pane_RightTapped;
				element.PointerPressed += Pane_PointerPressed;
			}
		}

		private void Pane_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is UIElement element)
				element.Focus(FocusState.Pointer);
		}

		private void Pane_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is ModernShellPage shellPage)
			{
				shellPage.SlimContentPage.LockPreviewPaneContent = true;
				shellPage.SlimContentPage.ItemManipulationModel.ClearSelection();
				shellPage.SlimContentPage.LockPreviewPaneContent = false;
				ActivePane = shellPage;
			}
		}

		private void Pane_RightTapped(object sender, RoutedEventArgs e)
		{
			if (sender != ActivePane && sender is UIElement element)
				element.Focus(FocusState.Programmatic);
		}

		private void Sizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var paneColumns = RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 1);
			paneColumns?.ForEach(x => x.Width = new GridLength(1, GridUnitType.Star));
		}

		private void Sizer_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is GridSplitter sizer)
				sizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void Sizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void Sizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
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

			// Dispose panes
			for (int index = 0; index < GetPaneCount(); index++)
				GetPane(index)?.Dispose();

			// Dispose sizers
			foreach (var sizer in RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 0)?.Cast<GridSplitter>())
			{
				sizer.Loaded += Pane_Loaded;
				sizer.GotFocus += Pane_GotFocus;
				sizer.RightTapped += Pane_RightTapped;
				sizer.PointerPressed += Pane_PointerPressed;
			}
		}
	}
}
