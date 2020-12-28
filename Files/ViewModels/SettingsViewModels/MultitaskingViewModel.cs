using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class MultitaskingViewModel : ObservableObject
    {
        private bool isMultitaskingExperienceAdaptive = App.AppSettings.IsMultitaskingExperienceAdaptive;
        private bool isHorizontalTabStripEnabled = App.AppSettings.IsHorizontalTabStripEnabled;
        private bool isVerticalTabFlyoutEnabled = App.AppSettings.IsVerticalTabFlyoutEnabled;

        public bool IsMultitaskingExperienceAdaptive
        {
            get
            {
                return isMultitaskingExperienceAdaptive;
            }
            set
            {
                if (SetProperty(ref isMultitaskingExperienceAdaptive, value))
                {
                    App.AppSettings.IsMultitaskingExperienceAdaptive = isMultitaskingExperienceAdaptive;
                }
            }
        }

        public bool IsHorizontalTabStripEnabled
        {
            get
            {
                return isHorizontalTabStripEnabled;
            }
            set
            {
                if (SetProperty(ref isHorizontalTabStripEnabled, value))
                {
                    App.AppSettings.IsHorizontalTabStripEnabled = isHorizontalTabStripEnabled;
                }
            }
        }

        public bool IsVerticalTabFlyoutEnabled
        {
            get
            {
                return isVerticalTabFlyoutEnabled;
            }
            set
            {
                if (SetProperty(ref isHorizontalTabStripEnabled, value))
                {
                    App.AppSettings.IsVerticalTabFlyoutEnabled = isVerticalTabFlyoutEnabled;
                }
            }
        }
    }
}
