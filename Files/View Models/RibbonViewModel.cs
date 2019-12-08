using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.UI.Xaml.Controls;
using System;

namespace Files.Controls
{
    public class RibbonViewModel : ViewModelBase
    {
        private bool _ShowRibbonContent = true;
        private string _ToggleRibbonIcon = "";
        private CommandBarLabelPosition _ItemLabelPosition = CommandBarLabelPosition.Default;
        public string ToggleRibbonIcon
        {
            get => _ToggleRibbonIcon;
            set => Set(ref _ToggleRibbonIcon, value);
        }

        public bool ShowRibbonContent
        {
            get => _ShowRibbonContent;
            set => Set(ref _ShowRibbonContent, value);
        }

        public CommandBarLabelPosition ItemLabelPosition
        {
            get => _ItemLabelPosition;
            set => Set(ref _ItemLabelPosition, value);
        }

        private RelayCommand toggleRibbon;
        public RelayCommand ToggleRibbon => toggleRibbon = new RelayCommand(() =>
        {
            ShowRibbonContent = !ShowRibbonContent;

            UpdateToggleIcon();
        });

        private RelayCommand showRibbonCommand;
        public RelayCommand ShowRibbonCommand => showRibbonCommand = new RelayCommand(() =>
        {
            if (ShowRibbonContent == false)
            {
                ShowRibbonContent = true;
                
                UpdateToggleIcon();
            }
        });

        public void UpdateToggleIcon()
        {
            if (ShowRibbonContent)
            {
                ToggleRibbonIcon = ""; //This is the hide icon
            }
            else
            {
                ToggleRibbonIcon = ""; //This is the show icon
            }
        }

        public void HideItemLabels()
        {
            ItemLabelPosition = CommandBarLabelPosition.Collapsed;
        }

        public void ShowItemLabels()
        {
            ItemLabelPosition = CommandBarLabelPosition.Default;
        }
    }
}
