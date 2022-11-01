using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.UserControls.MultitaskingControl;
using Files.App.Views;
using Files.Backend.Extensions;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels
{
	public class MainPageViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public IMultitaskingControl? MultitaskingControl { get; set; }
		public List<IMultitaskingControl> MultitaskingControls { get; } = new List<IMultitaskingControl>();

		public static ObservableCollection<TabItem> AppInstances { get; private set; } = new ObservableCollection<TabItem>();

		private TabItem? selectedTabItem;
		public TabItem? SelectedTabItem
		{
			get => selectedTabItem;
			set => SetProperty(ref selectedTabItem, value);
		}

		private bool isWindowCompactOverlay;
		public bool IsWindowCompactOverlay
		{
			get => isWindowCompactOverlay;
			set => SetProperty(ref isWindowCompactOverlay, value);
		}

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; private set; }

		public ICommand OpenNewWindowAcceleratorCommand { get; private set; }

		public ICommand CloseSelectedTabKeyboardAcceleratorCommand { get; private set; }

		public ICommand AddNewInstanceAcceleratorCommand { get; private set; }

		public ICommand ReopenClosedTabAcceleratorCommand { get; private set; }

		public ICommand OpenSettingsCommand { get; private set; }

		public MainPageViewModel()
		{
			// Create commands
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
			OpenNewWindowAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAccelerator);
			CloseSelectedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CloseSelectedTabKeyboardAccelerator);
			AddNewInstanceAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(AddNewInstanceAccelerator);
			ReopenClosedTabAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ReopenClosedTabAccelerator);
			OpenSettingsCommand = new RelayCommand(OpenSettings);
		}

		private void NavigateToNumberedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			int indexToSelect = 0;
			switch (e!.KeyboardAccelerator.Key)
			{
				case VirtualKey.Number1:
					indexToSelect = 0;
					break;

				case VirtualKey.Number2:
					indexToSelect = 1;
					break;

				case VirtualKey.Number3:
					indexToSelect = 2;
					break;

				case VirtualKey.Number4:
					indexToSelect = 3;
					break;

				case VirtualKey.Number5:
					indexToSelect = 4;
					break;

				case VirtualKey.Number6:
					indexToSelect = 5;
					break;

				case VirtualKey.Number7:
					indexToSelect = 6;
					break;

				case VirtualKey.Number8:
					indexToSelect = 7;
					break;

				case VirtualKey.Number9:
					// Select the last tab
					indexToSelect = AppInstances.Count - 1;
					break;

				case VirtualKey.Tab:
					bool shift = e.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);

					if (!shift) // ctrl + tab, select next tab
					{
						if ((App.AppModel.TabStripSelectedIndex + 1) < AppInstances.Count)
							indexToSelect = App.AppModel.TabStripSelectedIndex + 1;
						else
							indexToSelect = 0;
					}
					else // ctrl + shift + tab, select previous tab
					{
						if ((App.AppModel.TabStripSelectedIndex - 1) >= 0)
							indexToSelect = App.AppModel.TabStripSelectedIndex - 1;
						else
							indexToSelect = AppInstances.Count - 1;
					}

					break;
			}

			// Only select the tab if it is in the list
			if (indexToSelect < AppInstances.Count)
				App.AppModel.TabStripSelectedIndex = indexToSelect;
			e.Handled = true;
		}

		private async void OpenNewWindowAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			Uri filesUWPUri = new Uri("files-uwp:");
			await Launcher.LaunchUriAsync(filesUWPUri);
			e!.Handled = true;
		}

		private void CloseSelectedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			if (App.AppModel.TabStripSelectedIndex >= AppInstances.Count)
			{
				TabItem tabItem = AppInstances[AppInstances.Count - 1];
				MultitaskingControl?.CloseTab(tabItem);
			}
			else
			{
				TabItem tabItem = AppInstances[App.AppModel.TabStripSelectedIndex];
				MultitaskingControl?.CloseTab(tabItem);
			}
			e!.Handled = true;
		}

		private async void AddNewInstanceAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			await AddNewTabAsync();
			e!.Handled = true;
		}

		private void ReopenClosedTabAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			(MultitaskingControl as BaseMultitaskingControl)?.ReopenClosedTab(null, null);
			e!.Handled = true;
		}

		private async void OpenSettings()
		{
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var dialog = dialogService.GetDialog(new SettingsDialogViewModel());
			await dialog.TryShowAsync();
		}

		public static async Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1)
		{
			if (string.IsNullOrEmpty(path))
				path = "Home".GetLocalizedResource();

			// Support drives launched through jump list by stripping away the question mark at the end.
			if (path.EndsWith("\\?"))
				path = path.Remove(path.Length - 1);

			TabItem tabItem = new TabItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};
			tabItem.Control.NavigationArguments = new TabItemArguments()
			{
				InitialPageType = type,
				NavigationArg = path
			};
			tabItem.Control.ContentChanged += Control_ContentChanged;
			await UpdateTabInfo(tabItem, path);
			var index = atIndex == -1 ? AppInstances.Count : atIndex;
			AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async void UpdateInstanceProperties(object navigationArg)
		{
			string windowTitle = string.Empty;
			if (navigationArg is PaneNavigationArguments paneArgs)
			{
				if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
				{
					var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
					var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);
					windowTitle = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
				}
				else
				{
					(windowTitle, _, _) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
				}
			}
			else if (navigationArg is string pathArgs)
			{
				(windowTitle, _, _) = await GetSelectedTabInfoAsync(pathArgs);
			}
			if (AppInstances.Count > 1)
				windowTitle = $"{windowTitle} ({AppInstances.Count})";
			if (navigationArg == SelectedTabItem?.TabItemArguments?.NavigationArg)

				App.GetAppWindow(App.Window).Title = windowTitle;
		}

		public static async Task UpdateTabInfo(TabItem tabItem, object navigationArg)
		{
			tabItem.AllowStorageItemDrop = true;
			if (navigationArg is PaneNavigationArguments paneArgs)
			{
				if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
				{
					var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
					var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);
					tabItem.Header = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
					tabItem.IconSource = leftTabInfo.tabIcon;
				}
				else
				{
					(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
				}
			}
			else if (navigationArg is string pathArgs)
			{
				(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = await GetSelectedTabInfoAsync(pathArgs);
			}
		}

		public static async Task<(string tabLocationHeader, Microsoft.UI.Xaml.Controls.IconSource tabIcon, string toolTipText)> GetSelectedTabInfoAsync(string currentPath)
		{
			string? tabLocationHeader;
			var iconSource = new Microsoft.UI.Xaml.Controls.ImageIconSource();
			string toolTipText = currentPath;

			if (string.IsNullOrEmpty(currentPath) || currentPath == "Home".GetLocalizedResource())
			{
				tabLocationHeader = "Home".GetLocalizedResource();
				iconSource.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
			}
			else if (currentPath.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Desktop".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Downloads".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
			{
				var localSettings = ApplicationData.Current.LocalSettings;
				tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
			}
			else if (currentPath.Equals(CommonPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "SidebarNetworkDrives".GetLocalizedResource();
			}
			else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
			{
				var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path).GetLocalizedResource();
				// If localized string is empty use the library name.
				tabLocationHeader = string.IsNullOrEmpty(libName) ? library.Text : libName;
			}
			else
			{
				var matchingCloudDrive = App.CloudDrivesManager.Drives.FirstOrDefault(x => PathNormalization.NormalizePath(currentPath).Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
				if (matchingCloudDrive is not null)
				{
					tabLocationHeader = matchingCloudDrive.Text;
				}
				else if (PathNormalization.NormalizePath(PathNormalization.GetPathRoot(currentPath)) == PathNormalization.NormalizePath(currentPath)) // If path is a drive's root
				{
					var matchingNetDrive = App.NetworkDrivesManager.Drives.FirstOrDefault(x => PathNormalization.NormalizePath(currentPath).Contains(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
					if (matchingNetDrive is not null)
						tabLocationHeader = matchingNetDrive.Text;
					else
						tabLocationHeader = PathNormalization.NormalizePath(currentPath);
				}
				else
				{
					tabLocationHeader = currentPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

					FilesystemResult<StorageFolderWithPath> rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(currentPath));
					if (rootItem)
					{
						BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
						if (currentFolder is not null && !string.IsNullOrEmpty(currentFolder.DisplayName))
							tabLocationHeader = currentFolder.DisplayName;
					}
				}
			}

			if (iconSource.ImageSource is null)
			{
				var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView, true);
				if (iconData is not null)
					iconSource.ImageSource = await iconData.ToBitmapAsync();
			}

			return (tabLocationHeader, iconSource, toolTipText);
		}

		public async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
				return;

			//Initialize the static theme helper to capture a reference to this window
			//to handle theme changes without restarting the app
			ThemeHelper.Initialize();

			if (e.Parameter is null || (e.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.PreferencesSettingsService.LastSessionTabList is not null)
					{
						var items = new TabItemArguments[UserSettingsService.PreferencesSettingsService.LastSessionTabList.Count];
						for (int i = 0; i < items.Length; i++)
						{
							var tabArgs = TabItemArguments.Deserialize(UserSettingsService.PreferencesSettingsService.LastSessionTabList[i]);
							items[i] = tabArgs;
						}
						BaseMultitaskingControl.RecentlyClosedTabs.Add(items);
					}

					if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (UserSettingsService.PreferencesSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in UserSettingsService.PreferencesSettingsService.LastSessionTabList)
							{
								var tabArgs = TabItemArguments.Deserialize(tabArgsString);
								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
							}
						}

						if (!UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
						{
							UserSettingsService.PreferencesSettingsService.LastSessionTabList = null;
						}
					}
					else if (UserSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup)
					{
						if (UserSettingsService.PreferencesSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in UserSettingsService.PreferencesSettingsService.TabsOnStartupList)
								await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
						}
						else
						{
							await AddNewTabAsync();
						}
					}
					else if (UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
					{
						if (UserSettingsService.PreferencesSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in UserSettingsService.PreferencesSettingsService.LastSessionTabList)
							{
								var tabArgs = TabItemArguments.Deserialize(tabArgsString);
								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
							}
							var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home".GetLocalizedResource() };
							UserSettingsService.PreferencesSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
						}
						else
						{
							await AddNewTabAsync();
						}
					}
					else
					{
						await AddNewTabAsync();
					}
				}
				catch (Exception)
				{
					await AddNewTabAsync();
				}
			}
			else
			{
				if (e.Parameter is string navArgs)
					await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
				else if (e.Parameter is PaneNavigationArguments paneArgs)
					await AddNewTabByParam(typeof(PaneHolderPage), paneArgs);
				else if (e.Parameter is TabItemArguments tabArgs)
					await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
			}
		}

		public static Task AddNewTabAsync()
			=> AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalizedResource());

		public void AddNewTab()
			=> AddNewTabAsync();

		public static async void AddNewTabAtIndex(object sender, RoutedEventArgs e)
		{
			await AddNewTabAsync();
		}

		public static async void DuplicateTabAtIndex(object sender, RoutedEventArgs e)
		{
			var tabItem = (TabItem)((FrameworkElement)sender).DataContext;
			var index = AppInstances.IndexOf(tabItem);

			if (AppInstances[index].TabItemArguments is not null)
			{
				var tabArgs = AppInstances[index].TabItemArguments;
				await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg, index + 1);
			}
			else
			{
				await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalizedResource());
			}
		}

		public static async Task AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
		{
			TabItem tabItem = new TabItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};

			tabItem.Control.NavigationArguments = new TabItemArguments()
			{
				InitialPageType = type,
				NavigationArg = tabViewItemArgs
			};

			tabItem.Control.ContentChanged += Control_ContentChanged;
			await UpdateTabInfo(tabItem, tabViewItemArgs);
			var index = atIndex == -1 ? AppInstances.Count : atIndex;
			AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public static async void Control_ContentChanged(object? sender, TabItemArguments e)
		{
			if (sender is null)
				return;
			TabItem? matchingTabItem = AppInstances.SingleOrDefault(x => x.Control == (TabItemControl)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfo(matchingTabItem, e.NavigationArg);
		}
	}
}
