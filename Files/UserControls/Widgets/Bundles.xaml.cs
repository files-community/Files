using System;
using Windows.UI.Xaml.Controls;
using Files.ViewModels.Widgets.Bundles;
using Files.ViewModels.Widgets;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Widgets
{
    public sealed partial class Bundles : UserControl, IWidgetItemModel, IDisposable
    {
        public BundlesViewModel ViewModel
        {
            get => (BundlesViewModel)DataContext;
            private set => DataContext = value;
        }

        public string WidgetName => nameof(Bundles);

        public bool IsWidgetSettingEnabled => App.AppSettings.ShowBundlesWidget;

        public Bundles()
        {
            this.InitializeComponent();

            this.ViewModel = new BundlesViewModel();
        }

        #region IDisposable

        public void Dispose()
        {
            // We need dispose to unhook events to avoid memory leaks
            this.ViewModel?.Dispose();

            this.ViewModel = null;
        }

        #endregion IDisposable
    }
}