using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class MultitaskingViewModel : ObservableObject
    {
        private bool isMultitaskingExperienceAdaptive = App.AppSettings.IsMultitaskingExperienceAdaptive;
        private bool isHorizontalTabStripOn = App.AppSettings.IsHorizontalTabStripOn;
        private bool isVerticalTabFlyoutOn = App.AppSettings.IsVerticalTabFlyoutOn;

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
                    App.AppSettings.IsMultitaskingExperienceAdaptive = value;
                }
            }
        }

        public bool IsHorizontalTabStripOn
        {
            get
            {
                return isHorizontalTabStripOn;
            }
            set
            {
                if (SetProperty(ref isHorizontalTabStripOn, value))
                {
                    App.AppSettings.IsHorizontalTabStripOn = value;
                }
            }
        }

        public bool IsVerticalTabFlyoutOn
        {
            get
            {
                return isVerticalTabFlyoutOn;
            }
            set
            {
                if (SetProperty(ref isHorizontalTabStripOn, value))
                {
                    App.AppSettings.IsVerticalTabFlyoutOn = value;
                }
            }
        }

        private bool isDualPaneEnabled = App.AppSettings.IsDualPaneEnabled;
        private bool alwaysOpenDualPaneInNewTab = App.AppSettings.AlwaysOpenDualPaneInNewTab;

        public bool IsDualPaneEnabled
        {
            get
            {
                return isDualPaneEnabled;
            }
            set
            {
                if (SetProperty(ref isDualPaneEnabled, value))
                {
                    App.AppSettings.IsDualPaneEnabled = value;
                }
            }
        }

        public bool AlwaysOpenDualPaneInNewTab
        {
            get
            {
                return alwaysOpenDualPaneInNewTab;
            }
            set
            {
                if (SetProperty(ref alwaysOpenDualPaneInNewTab, value))
                {
                    App.AppSettings.AlwaysOpenDualPaneInNewTab = value;
                }
            }
        }
    }
}