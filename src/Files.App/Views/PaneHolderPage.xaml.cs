using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.UserControls.MultitaskingControl;
using Files.App.Views.LayoutModes;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class PaneHolderPage : Page, IPaneHolder, ITabItemContent
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public bool IsLeftPaneActive => ActivePane == PaneLeft;
		public bool IsRightPaneActive => ActivePane == PaneRight;

		public event EventHandler<TabItemArguments> ContentChanged;

		public event PropertyChangedEventHandler PropertyChanged;

		public IFilesystemHelpers FilesystemHelpers => ActivePane?.FilesystemHelpers;

		private TabItemArguments tabItemArguments;

		public TabItemArguments TabItemArguments
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

		private bool _windowIsCompact = App.Window.Bounds.Width <= 750;

		private bool windowIsCompact
		{
			get => _windowIsCompact;
			set
			{
				if (value != _windowIsCompact)
				{
					_windowIsCompact = value;
					if (value)
					{
						wasRightPaneVisible = isRightPaneVisible;
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

		public bool IsMultiPaneActive => IsRightPaneVisible;

		public bool IsMultiPaneEnabled
		{
			get => UserSettingsService.PreferencesSettingsService.IsDualPaneEnabled && !(App.Window.Bounds.Width <= 750);
		}

		private NavigationParams navParamsLeft;

		public NavigationParams NavParamsLeft
		{
			get => navParamsLeft;
			set
			{
				if (navParamsLeft != value)
				{
					navParamsLeft = value;
					NotifyPropertyChanged(nameof(NavParamsLeft));
				}
			}
		}

		private NavigationParams navParamsRight;

		public NavigationParams NavParamsRight
		{
			get => navParamsRight;
			set
			{
				if (navParamsRight != value)
				{
					navParamsRight = value;
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
					{
						PaneRight.IsCurrentInstance = false;
					}
					if (ActivePane is not null)
					{
						ActivePane.IsCurrentInstance = isCurrentInstance;
					}
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
				{
					return (ActivePane.SlimContentPage as ColumnViewBrowser).ActiveColumnShellPage;
				}

				return ActivePane ?? PaneLeft;
			}
		}

		private bool isRightPaneVisible;

		public bool IsRightPaneVisible
		{
			get => isRightPaneVisible;
			set
			{
				if (value != isRightPaneVisible)
				{
					isRightPaneVisible = value;
					if (!isRightPaneVisible)
					{
						ActivePane = PaneLeft;
					}
					Pane_ContentChanged(null, null);
					NotifyPropertyChanged(nameof(IsRightPaneVisible));
					NotifyPropertyChanged(nameof(IsMultiPaneActive));
				}
			}
		}

		private bool isCurrentInstance;

		public bool IsCurrentInstance
		{
			get => isCurrentInstance;
			set
			{
				isCurrentInstance = value;
				PaneLeft.IsCurrentInstance = false;
				if (PaneRight is not null)
				{
					PaneRight.IsCurrentInstance = false;
				}
				if (ActivePane is not null)
				{
					ActivePane.IsCurrentInstance = value;
				}
			}
		}

		public const VirtualKey PlusKey = (VirtualKey)187;

		public PaneHolderPage()
		{
			InitializeComponent();
			App.Window.SizeChanged += Current_SizeChanged;
			ActivePane = PaneLeft;
			IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.PreferencesSettingsService.AlwaysOpenDualPaneInNewTab;
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

			// TODO: fallback / error when failed to get NavigationViewCompactPaneLength value?
		}

		private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(UserSettingsService.PreferencesSettingsService.IsDualPaneEnabled):
					NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
					break;
			}
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			windowIsCompact = App.Window.Bounds.Width <= 750;
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			if (eventArgs.Parameter is string navPath)
			{
				NavParamsLeft = new NavigationParams { NavPath = navPath };
				NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalizedResource() };
			}
			else if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
			{
				NavParamsLeft = new NavigationParams
				{
					NavPath = paneArgs.LeftPaneNavPathParam,
					SelectItem = paneArgs.LeftPaneSelectItemParam
				};
				NavParamsRight = new NavigationParams
				{
					NavPath = paneArgs.RightPaneNavPathParam,
					SelectItem = paneArgs.RightPaneSelectItemParam
				};
				IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam is not null;
			}

			TabItemArguments = new TabItemArguments()
			{
				InitialPageType = typeof(PaneHolderPage),
				NavigationArg = new PaneNavigationArguments()
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
			{
				IsRightPaneVisible = false;
			}
		}

		private void Pane_ContentChanged(object sender, TabItemArguments e)
		{
			TabItemArguments = new TabItemArguments()
			{
				InitialPageType = typeof(PaneHolderPage),
				NavigationArg = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = PaneLeft.TabItemArguments?.NavigationArg as string ?? e?.NavigationArg as string,
					RightPaneNavPathParam = IsRightPaneVisible ? PaneRight?.TabItemArguments?.NavigationArg as string : null
				}
			};
		}

		public Task TabItemDragOver(object sender, DragEventArgs e) => ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;

		public Task TabItemDrop(object sender, DragEventArgs e) => ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;

		public void OpenPathInNewPane(string path)
		{
			IsRightPaneVisible = true;
			NavParamsRight = new NavigationParams { NavPath = path };
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
					if (UserSettingsService.PreferencesSettingsService.IsDualPaneEnabled)
					{
						ActivePane = PaneLeft;
					}
					break;

				case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
					if (UserSettingsService.PreferencesSettingsService.IsDualPaneEnabled)
					{
						if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
						{
							NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalizedResource() };
						}
						IsRightPaneVisible = true;
						ActivePane = PaneRight;
					}
					break;

				case (true, true, false, VirtualKey.W): // ctrl + shift + "W" close right pane
					IsRightPaneVisible = false;
					break;

				case (false, true, true, VirtualKey.Add): // alt + shift + "+" open pane
				case (false, true, true, PlusKey):
					if (UserSettingsService.PreferencesSettingsService.IsDualPaneEnabled)
					{
						if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
						{
							NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalizedResource() };
						}
						IsRightPaneVisible = true;
					}
					break;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void CloseActivePane()
		{
			// Can only close right pane atm
			IsRightPaneVisible = false;
		}

		private void PaneLeft_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement).GotFocus += Pane_GotFocus;
		}

		private void PaneRight_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement).GotFocus += Pane_GotFocus;
		}

		private void Pane_GotFocus(object sender, RoutedEventArgs e)
		{
			ActivePane = sender == PaneLeft ? PaneLeft : PaneRight;
		}

		public void Dispose()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
			App.Window.SizeChanged -= Current_SizeChanged;
			PaneLeft?.Dispose();
			PaneRight?.Dispose();
			PaneResizer.DoubleTapped -= PaneResizer_OnDoubleTapped;
		}

		private void PaneResizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			LeftColumn.Width = new GridLength(1, GridUnitType.Star);
			RightColumn.Width = new GridLength(1, GridUnitType.Star);
		}
	}

	public class PaneNavigationArguments
	{
		public string LeftPaneNavPathParam { get; set; } = null;
		public string LeftPaneSelectItemParam { get; set; } = null;
		public string RightPaneNavPathParam { get; set; } = null;
		public string RightPaneSelectItemParam { get; set; } = null;
	}
}