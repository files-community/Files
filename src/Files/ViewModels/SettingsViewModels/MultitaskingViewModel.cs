using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;

namespace Files.ViewModels.SettingsViewModels
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