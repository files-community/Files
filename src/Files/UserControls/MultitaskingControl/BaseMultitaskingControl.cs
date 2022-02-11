using Files.Helpers;
using Files.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Backend.ViewModels.Shell.Multitasking;

namespace Files.UserControls.MultitaskingControl
{
    public abstract class BaseMultitaskingControl : UserControl
    {
        public MultitaskingControlViewModel ViewModel
        {
            get => (MultitaskingControlViewModel)DataContext;
            set => DataContext = value;
        }


        private static bool isRestoringClosedTab = false; // Avoid reopening two tabs

        public BaseMultitaskingControl()
        {
        }

        // RecentlyClosedTabs is shared between all multitasking controls
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