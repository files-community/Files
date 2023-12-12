// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class PaneHolderPage : Page, IPaneHolder, ITabBarItemContent
	{
		public static readonly int DualPaneWidthThreshold = 750;

		public static event EventHandler<PaneHolderPage>? CurrentInstanceChanged;

		private IUserSettingsService UserSettingsService { get; }

		public bool IsLeftPaneActive
			=> ActivePane == PaneLeft;

		public bool IsRightPaneActive
			=> ActivePane == PaneRight;

		public event EventHandler<CustomTabViewItemParameter> ContentChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public IFilesystemHelpers FilesystemHelpers
			=> ActivePane?.FilesystemHelpers;

		private CustomTabViewItemParameter tabItemArguments;
		public CustomTabViewItemParameter TabItemParameter
		{
			get => tabItemArguments;
			set
			{
				if (tabItemArguments != value)
				{
					tabItemArguments = value;

					ContentChanged?.Invoke(this, value);
				}
			}
		}

		private bool _WindowIsCompact = MainWindow.Instance.Bounds.Width <= DualPaneWidthThreshold;
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
						wasRightPaneVisible = IsRightPaneVisible;
						IsRightPaneVisible = false;
					}
					else if (wasRightPaneVisible)
					{
						IsRightPaneVisible = true;
						wasRightPaneVisible = false;
					}

					NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
				}
			}
		}

		private bool wasRightPaneVisible;

		public bool IsMultiPaneActive
			=> IsRightPaneVisible;

		public bool IsMultiPaneEnabled
		{
			get
			{
				if (App.AppModel.IsMainWindowClosed)
					return false;
				else
					return MainWindow.Instance.Bounds.Width > DualPaneWidthThreshold;
			}
		}

		private NavigationParams _NavParamsLeft;
		public NavigationParams NavParamsLeft
		{
			get => _NavParamsLeft;
			set
			{
				if (_NavParamsLeft != value)
				{
					_NavParamsLeft = value;

					NotifyPropertyChanged(nameof(NavParamsLeft));
				}
			}
		}

		private NavigationParams _NavParamsRight;
		public NavigationParams NavParamsRight
		{
			get => _NavParamsRight;
			set
			{
				if (_NavParamsRight != value)
				{
					_NavParamsRight = value;

					NotifyPropertyChanged(nameof(NavParamsRight));
				}
			}
		}

		private IShellPage activePane;
		public IShellPage ActivePane
		{
			get => activePane;
			set
			{
				if (activePane != value)
				{
					activePane = value;

					PaneLeft.IsCurrentInstance = false;

					if (PaneRight is not null)
						PaneRight.IsCurrentInstance = false;
					if (ActivePane is not null)
						ActivePane.IsCurrentInstance = IsCurrentInstance;

					NotifyPropertyChanged(nameof(ActivePane));
					NotifyPropertyChanged(nameof(IsLeftPaneActive));
					NotifyPropertyChanged(nameof(IsRightPaneActive));
					NotifyPropertyChanged(nameof(ActivePaneOrColumn));
					NotifyPropertyChanged(nameof(FilesystemHelpers));
				}
			}
		}

		public IShellPage ActivePaneOrColumn
		{
			get
			{
				if (ActivePane is not null && ActivePane.IsColumnView)
					return (ActivePane.SlimContentPage as ColumnsLayoutPage).ActiveColumnShellPage;

				return ActivePane ?? PaneLeft;
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
						ActivePane = PaneLeft;

					Pane_ContentChanged(null, null);
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
				PaneLeft.IsCurrentInstance = false;

				if (PaneRight is not null)
					PaneRight.IsCurrentInstance = false;

				if (ActivePane is not null)
				{
					ActivePane.IsCurrentInstance = value;

					if (value && ActivePane is BaseShellPage baseShellPage)
						baseShellPage.ContentPage?.ItemManipulationModel.FocusFileList();
				}

				CurrentInstanceChanged?.Invoke(null, this);
			}
		}

		public PaneHolderPage()
		{
			InitializeComponent();

			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			MainWindow.Instance.SizeChanged += Current_SizeChanged;
			ActivePane = PaneLeft;
			IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;

			// TODO?: Fallback or an error can occur when failing to get NavigationViewCompactPaneLength value
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowIsCompact = MainWindow.Instance.Bounds.Width <= DualPaneWidthThreshold;
		}

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

			TabItemParameter = new()
			{
				InitialPageType = typeof(PaneHolderPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = NavParamsLeft?.NavPath,
					LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
					RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight?.NavPath : null,
					RightPaneSelectItemParam = IsRightPaneVisible ? NavParamsRight?.SelectItem : null,
				}
			};
		}

		private void PaneResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (PaneRight is not null && PaneRight.ActualWidth <= 300)
				IsRightPaneVisible = false;

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void Pane_ContentChanged(object sender, CustomTabViewItemParameter e)
		{
			TabItemParameter = new()
			{
				InitialPageType = typeof(PaneHolderPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = PaneLeft.TabItemParameter?.NavigationParameter as string ?? e?.NavigationParameter as string,
					RightPaneNavPathParam = IsRightPaneVisible ? PaneRight?.TabItemParameter?.NavigationParameter as string : null
				}
			};
		}

		public Task TabItemDragOver(object sender, DragEventArgs e)
			=> ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;

		public Task TabItemDrop(object sender, DragEventArgs e)
			=> ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;

		public void OpenPathInNewPane(string path)
		{
			IsRightPaneVisible = true;
			NavParamsRight = new() { NavPath = path };
			ActivePane = PaneRight;
		}

		private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var menu = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

			switch (c: ctrl, s: shift, m: menu, k: args.KeyboardAccelerator.Key)
			{
				case (true, true, false, VirtualKey.Left): // ctrl + shift + "<-" select left pane
					ActivePane = PaneLeft;
					break;

				case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
					if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
					{
						NavParamsRight = new NavigationParams { NavPath = "Home" };
					}
					IsRightPaneVisible = true;
					ActivePane = PaneRight;
					break;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void CloseActivePane()
		{
			// NOTE: Can only close right pane at the moment
			IsRightPaneVisible = false;
			PaneLeft.Focus(FocusState.Programmatic);
		}

		private void Pane_Loaded(object sender, RoutedEventArgs e)
		{
			((UIElement)sender).GotFocus += Pane_GotFocus;
			((UIElement)sender).RightTapped += Pane_RightTapped;
		}

		private async void Pane_GotFocus(object sender, RoutedEventArgs e)
		{
			var isLeftPane = sender == PaneLeft;
			if (isLeftPane && (PaneRight?.SlimContentPage?.IsItemSelected ?? false))
			{
				PaneRight.SlimContentPage.LockPreviewPaneContent = true;
				PaneRight.SlimContentPage.ItemManipulationModel.ClearSelection();
				PaneRight.SlimContentPage.LockPreviewPaneContent = false;
			}
			else if (!isLeftPane && (PaneLeft?.SlimContentPage?.IsItemSelected ?? false))
			{
				PaneLeft.SlimContentPage.LockPreviewPaneContent = true;
				PaneLeft.SlimContentPage.ItemManipulationModel.ClearSelection();
				PaneLeft.SlimContentPage.LockPreviewPaneContent = false;
			}

			var activePane = isLeftPane ? PaneLeft : PaneRight;
			if (ActivePane != activePane)
				ActivePane = activePane;
		}

		private void Pane_RightTapped(object sender, RoutedEventArgs e)
		{
			if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				((UIElement)sender).Focus(FocusState.Programmatic);
		}

		public void Dispose()
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
			PaneLeft?.Dispose();
			PaneRight?.Dispose();
			PaneResizer.DoubleTapped -= PaneResizer_OnDoubleTapped;
		}

		private void PaneResizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			LeftColumn.Width = new GridLength(1, GridUnitType.Star);
			RightColumn.Width = new GridLength(1, GridUnitType.Star);
		}

		private void PaneResizer_Loaded(object sender, RoutedEventArgs e)
		{
			PaneResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void PaneResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}
	}
}
