using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Controls
{
    public class InteractionViewModel : ObservableObject
    {
        private bool _IsContentLoadingIndicatorVisible = false;

        public bool IsContentLoadingIndicatorVisible
        {
            get => _IsContentLoadingIndicatorVisible;
            set => SetProperty(ref _IsContentLoadingIndicatorVisible, value);
        }

        private int _TabStripSelectedIndex = 0;

        public int TabStripSelectedIndex
        {
            get => _TabStripSelectedIndex;
            set
            {
                if (value >= 0)
                {
                    SetProperty(ref _TabStripSelectedIndex, value);
                    Frame rootFrame = Window.Current.Content as Frame;
                    var mainView = rootFrame.Content as MainPage;
                    mainView.SelectedTabItem = App.CurrentInstance.MultitaskingControl.Items[value];
                }
            }
        }

        private Thickness _TabsLeftMargin = new Thickness(0, 0, 0, 0);

        public Thickness TabsLeftMargin
        {
            get => _TabsLeftMargin;
            set => SetProperty(ref _TabsLeftMargin, value);
        }

        private bool _LeftMarginLoaded = true;

        public bool LeftMarginLoaded
        {
            get => _LeftMarginLoaded;
            set => SetProperty(ref _LeftMarginLoaded, value);
        }

        private bool _isPasteEnabled = false;

        public bool IsPasteEnabled
        {
            get => _isPasteEnabled;
            set => SetProperty(ref _isPasteEnabled, value);
        }
    }
}