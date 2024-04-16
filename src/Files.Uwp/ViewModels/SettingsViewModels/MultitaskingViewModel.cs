using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Files.Uwp.ViewModels.SettingsViewModels
{
    public class MultitaskingViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool IsVerticalTabFlyoutEnabled
        {
            get => UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled;
            set
            {
                if (value != UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled)
                {
                    UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDualPaneEnabled
        {
            get => UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled;
            set
            {
                if (value != UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                {
                    UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AlwaysOpenDualPaneInNewTab
        {
            get => UserSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab;
            set
            {
                if (value != UserSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab)
                {
                    UserSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}