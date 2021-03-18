using Files.Helpers;
using Files.UserControls.MultitaskingControl;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Linq;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using static Files.Views.MainPage;

namespace Files.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        #region Private Members

        private bool isRestoringClosedTab = false; // Avoid reopening two tabs

        #endregion

        #region Public Properties

        private TabItem selectedTabItem;

        public TabItem SelectedTabItem
        {
            get => selectedTabItem;
            set => SetProperty(ref selectedTabItem, value);
        }

        #endregion

        #region Commands

        public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; private set; }

        public ICommand OpenNewWindowAcceleratorCommand { get; private set; }

        public ICommand CloseSelectedTabKeyboardAcceleratorCommand { get; private set; }

        public ICommand AddNewInstanceAcceleratorCommand { get; private set; }

        #endregion

        #region Constructor

        public MainPageViewModel()
        {
            // Create commands
            NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
            OpenNewWindowAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAccelerator);
            CloseSelectedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CloseSelectedTabKeyboardAccelerator);
            AddNewInstanceAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(AddNewInstanceAccelerator);
        }

        #endregion

        #region Command Implementation

        private void NavigateToNumberedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            int indexToSelect = 0;

            switch (e.KeyboardAccelerator.Key)
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
            }

            // Only select the tab if it is in the list
            if (indexToSelect < AppInstances.Count)
            {
                App.InteractionViewModel.TabStripSelectedIndex = indexToSelect;
            }
            e.Handled = true;
        }

        private async void OpenNewWindowAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            e.Handled = true;
            Uri filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void CloseSelectedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (App.InteractionViewModel.TabStripSelectedIndex >= AppInstances.Count)
            {
                TabItem tabItem = AppInstances[AppInstances.Count - 1];
                MultitaskingControl?.RemoveTab(tabItem);
            }
            else
            {
                TabItem tabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
                MultitaskingControl?.RemoveTab(tabItem);
            }
            e.Handled = true;
        }

        private async void AddNewInstanceAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            bool shift = e.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);

            if (!shift)
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
            else // ctrl + shift + t, restore recently closed tab
            {
                if (!isRestoringClosedTab && MultitaskingControl.RecentlyClosedTabs.Any())
                {
                    isRestoringClosedTab = true;
                    ITabItem lastTab = MultitaskingControl.RecentlyClosedTabs.Last();
                    MultitaskingControl.RecentlyClosedTabs.Remove(lastTab);
                    await AddNewTabByParam(lastTab.TabItemArguments.InitialPageType, lastTab.TabItemArguments.NavigationArg);
                    isRestoringClosedTab = false;
                }
            }
            e.Handled = true;
        }

        #endregion

        #region Public Helpers

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode != NavigationMode.Back)
            {
                //Initialize the static theme helper to capture a reference to this window
                //to handle theme changes without restarting the app
                ThemeHelper.Initialize();

                if (e.Parameter == null || (e.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
                {
                    try
                    {
                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                            {
                                var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                            }

                            if (!App.AppSettings.ContinueLastSessionOnStartUp)
                            {
                                App.AppSettings.LastSessionPages = null;
                            }
                        }
                        else if (App.AppSettings.OpenASpecificPageOnStartup)
                        {
                            if (App.AppSettings.PagesOnStartupList != null)
                            {
                                foreach (string path in App.AppSettings.PagesOnStartupList)
                                {
                                    await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                                }
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else if (App.AppSettings.ContinueLastSessionOnStartUp)
                        {
                            if (App.AppSettings.LastSessionPages != null)
                            {
                                foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                                {
                                    var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                    await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                                }
                                var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "NewTab".GetLocalized() };
                                App.AppSettings.LastSessionPages = new string[] { defaultArg.Serialize() };
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
                    {
                        await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
                    }
                    else if (e.Parameter is TabItemArguments tabArgs)
                    {
                        await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                    }
                }

                // Check for required updates
                AppUpdater updater = new AppUpdater();
                updater.CheckForUpdatesAsync();

                // Initial setting of SelectedTabItem
                SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        #endregion
    }
}
