using Files.Uwp.UserControls.MultitaskingControl;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Helpers
{
    public static class MultitaskingTabsHelpers
    {
        public static void CloseTabsToTheLeft(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static void CloseTabsToTheRight(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static void CloseOtherTabs(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static async Task MoveTabToNewWindow(TabItem tab, IMultitaskingControl multitaskingControl)
        {
            int index = MainPageViewModel.AppInstances.IndexOf(tab);
            TabItemArguments tabItemArguments = MainPageViewModel.AppInstances[index].TabItemArguments;

            multitaskingControl?.CloseTab(MainPageViewModel.AppInstances[index]);

            if (tabItemArguments != null)
            {
                await NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize());
            }
            else
            {
                await NavigationHelpers.OpenPathInNewWindowAsync("Home".GetLocalized());
            }
        }

        public static async Task AddNewTab(Type type, object tabViewItemArgs, int atIndex = -1)
        {
            FontIconSource fontIconSource = new FontIconSource();
            fontIconSource.FontFamily = App.MainViewModel.FontName;

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = tabViewItemArgs
            };
            tabItem.Control.ContentChanged += MainPageViewModel.Control_ContentChanged;
            await MainPageViewModel.UpdateTabInfo(tabItem, tabViewItemArgs);
            MainPageViewModel.AppInstances.Insert(atIndex == -1 ? MainPageViewModel.AppInstances.Count : atIndex, tabItem);
        }
    }
}
