using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.Messages;
using Files.Backend.Services;
using Files.Backend.ViewModels.Shell.Tabs;
using Files.Shared.Extensions;

#nullable enable

namespace Files.Backend.ViewModels.Shell.Multitasking
{
    public class BaseMultitaskingControlViewModel
    {
        private IApplicationService ApplicationService { get; } = Ioc.Default.GetRequiredService<IApplicationService>();

        public ObservableCollection<TabItemViewModel> Tabs { get; }

        public BaseMultitaskingControlViewModel()
        {
            this.Tabs = new();
        }

        public void CloseTab(TabItemViewModel tabItemViewModel)
        {
            if (Tabs.Remove(tabItemViewModel))
            {
                if (Tabs.IsEmpty())
                {
                    ApplicationService.CloseApplication();
                }
                else
                {
                    tabItemViewModel.Dispose();
                }
            }
        }

        public TabItemViewModel AddTab(FuturisticShellPageViewModel futuristicShellPageViewModel, int index = -1)
        {
            index = index == -1 ? Tabs.Count : index;
            var tabItemViewModel = new TabItemViewModel(futuristicShellPageViewModel);

            Tabs.Insert(index, tabItemViewModel);
            WeakReferenceMessenger.Default.Send(new TabAddRequestedMessage(tabItemViewModel));

            return tabItemViewModel;
        }
    }
}
