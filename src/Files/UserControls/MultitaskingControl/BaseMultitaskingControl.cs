using Files.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Backend.ViewModels.Shell.Multitasking;

namespace Files.UserControls.MultitaskingControl
{
    [Obsolete]
    public abstract class BaseMultitaskingControl : UserControl
    {
        private static bool isRestoringClosedTab = false; // Avoid reopening two tabs

        public static List<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new List<TabItemArguments[]>();


        public async void ReopenClosedTab(object sender, RoutedEventArgs e)
        {
            if (!isRestoringClosedTab && RecentlyClosedTabs.Any())
            {
                isRestoringClosedTab = true;
                var lastTab = RecentlyClosedTabs.Last();
                RecentlyClosedTabs.Remove(lastTab);
                foreach (var item in lastTab)
                {
                    await MainPageViewModel.AddNewTabByParam(item.InitialPageType, item.NavigationArg);
                }
                isRestoringClosedTab = false;
            }
        }
    }
}