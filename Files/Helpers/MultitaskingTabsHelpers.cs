using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class MultitaskingTabsHelpers
    {
        public static async Task AddNewTab(Type type, object tabViewItemArgs, int atIndex = -1)
        {
            FontIconSource fontIconSource = new FontIconSource();
            fontIconSource.FontFamily = App.InteractionViewModel.FontName;

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

        public static void CloseTabsToTheRight(TabItem clickedTab)
        {
            int index = MainPageViewModel.AppInstances.IndexOf(clickedTab);
            List<TabItem> tabsToClose = new List<TabItem>();

            for (int i = index + 1; i < MainPageViewModel.AppInstances.Count; i++)
            {
                tabsToClose.Add(MainPageViewModel.AppInstances[i]);
            }

            foreach (var item in tabsToClose)
            {
                MainPageViewModel.MultitaskingControl?.CloseTab(item);
            }
        }

        public static async Task MoveTabToNewWindow(TabItem tab)
        {
            int index = MainPageViewModel.AppInstances.IndexOf(tab);
            TabItemArguments tabItemArguments = MainPageViewModel.AppInstances[index].TabItemArguments;

            MainPageViewModel.MultitaskingControl?.CloseTab(MainPageViewModel.AppInstances[index]);

            if (tabItemArguments != null)
            {
                await NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize());
            }
            else
            {
                await NavigationHelpers.OpenPathInNewWindowAsync("NewTab".GetLocalized());
            }
        }
    }
}