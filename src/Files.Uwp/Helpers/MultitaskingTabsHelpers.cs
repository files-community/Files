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
                var tabs = multitaskingControl.Items;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static void CloseTabsToTheRight(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = multitaskingControl.Items;
                var currentIndex = tabs.IndexOf(clickedTab);

                tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static void CloseOtherTabs(TabItem clickedTab, IMultitaskingControl multitaskingControl)
        {
            if (multitaskingControl is not null)
            {
                var tabs = multitaskingControl.Items;
                tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
            }
        }

        public static async Task MoveTabToNewWindow(TabItem tab, IMultitaskingControl multitaskingControl)
        {
            int index = multitaskingControl.Items.IndexOf(tab);
            TabItemArguments tabItemArguments = multitaskingControl.Items[index].TabItemArguments;

            multitaskingControl?.CloseTab(multitaskingControl.Items[index]);

            if (tabItemArguments != null)
            {
                await NavigationHelpers.OpenPathInNewWindowAsync(tabItemArguments.NavigationArg.ToString());
            }
            else
            {
                await NavigationHelpers.OpenPathInNewWindowAsync("Home".GetLocalized());
            }
        }
    }
}
