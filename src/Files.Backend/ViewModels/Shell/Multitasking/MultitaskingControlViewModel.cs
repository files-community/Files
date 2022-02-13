using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.EventArguments;
using Files.Backend.Messages;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Shell.Tabs;
using Files.Shared.Extensions;

#nullable enable

namespace Files.Backend.ViewModels.Shell.Multitasking
{
    public sealed class MultitaskingControlViewModel : ObservableObject, IRecipient<TabAddRequestedMessage>
    {
        private IApplicationService ApplicationService { get; } = Ioc.Default.GetRequiredService<IApplicationService>();

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

        public ObservableCollection<TabItemViewModel> Tabs { get; }

        private TabItemViewModel? _SelectedItem;
        public TabItemViewModel SelectedItem
        {
            get => _SelectedItem!;
            set => SetProperty(ref _SelectedItem, value); // TODO(i): Wake up a sleeping tab
        }

        public bool IsVerticalTabFlyoutEnabled
        {
            get => UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled;
        }

        public IRelayCommand AddTabCommand { get; }

        public MultitaskingControlViewModel()
        {
            this.Tabs = new();

            WeakReferenceMessenger.Default.Register<TabAddRequestedMessage>(this);

            this.Tabs.CollectionChanged += Tabs_CollectionChanged;
            this.UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            AddTabCommand = new RelayCommand(() => AddTab());
        }

        public TabItemViewModel AddTab(int index = -1)
        {
            var instanceMessenger = new WeakReferenceMessenger();
            var futuristicShellPageViewModel = new FuturisticShellPageViewModel(instanceMessenger);
            var tabItemViewModel = new TabItemViewModel(futuristicShellPageViewModel);

            return AddTab(tabItemViewModel, index);
        }

        public TabItemViewModel AddTab(TabItemViewModel tabItemViewModel, int index = -1)
        {
            index = index == -1 ? Tabs.Count : index;

            Tabs.Insert(index, tabItemViewModel);
            WeakReferenceMessenger.Default.Send(new TabAddRequestedMessage(tabItemViewModel));

            return tabItemViewModel;
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

        public void CloseTabsToTheRight(TabItemViewModel clickedTab)
        {
            var clickedTabIndex = Tabs.IndexOf(clickedTab);

            for (int i = clickedTabIndex + 1; i < Tabs.Count; i++)
            {
                CloseTab(Tabs[i]);
            }
        }

        public async Task OpenTabInNewWindow(TabItemViewModel tab)
        {
            if (await ApplicationService.OpenInNewWindowAsync(tab.TabShell.ActiveLayoutViewModel.SomeVeryNicePathThatPointsToSomeVeryNiceFolder))
            {
                CloseTab(tab);
            }
        }

        public void DuplicateTab()
        {
            throw new NotImplementedException();
        }

        private void Tabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                SelectedItem = this.Tabs.LastOrDefault()!;
                WeakReferenceMessenger.Default.Send(new TabInstanceChangedMessage(SelectedItem));
            }
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled):
                    OnPropertyChanged(nameof(IsVerticalTabFlyoutEnabled));
                    break;
            }
        }

        public void Receive(TabAddRequestedMessage message)
        {
            AddTab(message.Value);
        }
    }
}
