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
                    App.AppSettings.IsMultitaskingExperienceAdaptive = value;
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
                    App.AppSettings.IsHorizontalTabStripEnabled = value;
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
                    App.AppSettings.IsVerticalTabFlyoutEnabled = value;
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