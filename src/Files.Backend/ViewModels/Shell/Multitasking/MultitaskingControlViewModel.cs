using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.EventArguments;
using Files.Backend.Helpers;
using Files.Backend.Messages;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Shell.Tabs;
using Files.Shared.Extensions;

#nullable enable

namespace Files.Backend.ViewModels.Shell.Multitasking
{
    public sealed class MultitaskingControlViewModel : ObservableObject
    {
        private IApplicationService ApplicationService { get; } = Ioc.Default.GetRequiredService<IApplicationService>();
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public ObservableCollection<TabItemViewModel> Tabs { get; }

        public bool IsVerticalTabFlyoutEnabled;

        private TabItemViewModel? _SelectedItem;
        public TabItemViewModel SelectedItem
        {
            get => _SelectedItem!;
            set => SetProperty(ref _SelectedItem, value); // TODO(i): Wake up a sleeping tab
        }

        public IRelayCommand AddTabCommand { get; }

        public MultitaskingControlViewModel()
        {
            this.Tabs = new();

            this.Tabs.CollectionChanged += Tabs_CollectionChanged;
            AddTabCommand = new RelayCommand(AddTab);
            IsVerticalTabFlyoutEnabled = UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled;
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        private void Tabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                SelectedItem = this.Tabs.LastOrDefault();
            }
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled):
                    SetProperty<bool>(ref IsVerticalTabFlyoutEnabled, (bool)e.newValue);
                    break;
            }
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

        private void AddTab()
        {
            _ = AddTab(new());
        }

        public void CloseTabsToTheRight(TabItemViewModel clickedTab)
        {
            int index = Tabs.IndexOf(clickedTab);
            List<TabItemViewModel> tabsToClose = new List<TabItemViewModel>();

            for (int i = index + 1; i < Tabs.Count; i++)
            {
                tabsToClose.Add(clickedTab);
            }

            foreach (TabItemViewModel item in tabsToClose)
            {
                CloseTab(item);
            }
        }

        public async Task MoveTabToNewWindow(TabItemViewModel tab)
        {
            // TODO: Use FilesystemViewModel
            //await NavigationHelpers.OpenPathInNewWindowAsync(tab.TabShell.ActiveLayoutViewModel);
            throw new NotImplementedException();

            CloseTab(tab);
        }

        public async void DuplicateTab()
        {
            throw new NotImplementedException();
        }
    }
}
