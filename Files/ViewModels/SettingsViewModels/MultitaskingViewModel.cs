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
                if (isMultitaskingExperienceAdaptive != value)
                {
                    isMultitaskingExperienceAdaptive = value;
                    App.AppSettings.IsMultitaskingExperienceAdaptive = isMultitaskingExperienceAdaptive;
                    OnPropertyChanged(nameof(IsMultitaskingExperienceAdaptive));
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
                if (isHorizontalTabStripEnabled != value)
                {
                    isHorizontalTabStripEnabled = value;
                    App.AppSettings.IsHorizontalTabStripEnabled = isHorizontalTabStripEnabled;
                    OnPropertyChanged(nameof(IsHorizontalTabStripEnabled));
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
                if (isVerticalTabFlyoutEnabled != value)
                {
                    isVerticalTabFlyoutEnabled = value;
                    App.AppSettings.IsVerticalTabFlyoutEnabled = isVerticalTabFlyoutEnabled;
                    OnPropertyChanged(nameof(IsVerticalTabFlyoutEnabled));
                }
            }
        }
    }
}
