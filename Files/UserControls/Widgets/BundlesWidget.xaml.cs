using Files.ViewModels.Widgets;
using Files.ViewModels.Widgets.Bundles;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Widgets
{
    public sealed partial class BundlesWidget : UserControl, IWidgetItemModel, IDisposable
    {
        public BundlesViewModel ViewModel
        {
            get => (BundlesViewModel)DataContext;
            private set => DataContext = value;
        }

        public string WidgetName => nameof(BundlesWidget);

        public string AutomationProperties => "BundlesWidgetAutomationProperties/Name".GetLocalized();

        public bool IsWidgetSettingEnabled => App.AppSettings.ShowBundlesWidget;

        public BundlesWidget()
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