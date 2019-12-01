using ByteSizeLib;
using Files.Enums;
using Files.Interacts;
using Files.Navigation;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Controls
{
    public class RibbonViewModel : ViewModelBase
    {
        private bool _ShowRibbonContent = true;
        private string _ToggleRibbonIcon = "";

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
    }
}
