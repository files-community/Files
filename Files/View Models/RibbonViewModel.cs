using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.UI.Xaml.Controls;
using System;
using Files.Interacts;

namespace Files.Controls
{
    public class RibbonViewModel : ViewModelBase
    {
        private Windows.UI.Xaml.Visibility _AppBarSeparatorVisibility = Windows.UI.Xaml.Visibility.Visible;
        public Interacts.Home.HomeItemsState HomeItems { get; set; } = new Interacts.Home.HomeItemsState();
        public Interacts.Share.ShareItemsState ShareItems { get; set; } = new Interacts.Share.ShareItemsState();
        public Interacts.Layout.LayoutItemsState LayoutItems { get; set; } = new Interacts.Layout.LayoutItemsState();
        public AlwaysPresentCommandsState AlwaysPresentCommands { get; set; } = new AlwaysPresentCommandsState();

        public Windows.UI.Xaml.Visibility AppBarSeparatorVisibility
        {
            get => _AppBarSeparatorVisibility;
            set => Set(ref _AppBarSeparatorVisibility, value);
        }

        public void HideEachItemLabel()
        {
            throw new NotImplementedException();
        }

        public void ShowEachItemLabel()
        {
            throw new NotImplementedException();
        }

        public void HideAppBarSeparator()
        {
            AppBarSeparatorVisibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        public void ShowAppBarSeparator()
        {
            AppBarSeparatorVisibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}
