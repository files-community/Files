using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class MultitaskingTabsHelpers
    {
        public static void CloseTabsToTheLeft(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab, true));
            }
        }

        public static void CloseTabsToTheRight(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab, true));
            }
        }

        public static void CloseOtherTabs(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = MainPageViewModel.AppInstances;
                tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab, true));
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
    }
}
