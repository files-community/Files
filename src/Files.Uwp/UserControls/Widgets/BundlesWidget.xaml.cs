using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels.Widgets;
using Files.Uwp.ViewModels.Widgets.Bundles;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.Uwp.UserControls.Widgets
{
    public sealed partial class BundlesWidget : UserControl, IWidgetItemModel, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public BundlesViewModel ViewModel
        {
            get => (BundlesViewModel)DataContext;
            private set => DataContext = value;
        }

        public string WidgetName => nameof(BundlesWidget);

        public string AutomationProperties => "BundlesWidgetAutomationProperties/Name".GetLocalized();

        public string WidgetHeader => "Bundles".GetLocalized();

        public bool IsWidgetSettingEnabled => UserSettingsService.WidgetsSettingsService.ShowBundlesWidget;

        public BundlesWidget()
        {
            this.InitializeComponent();

            this.ViewModel = new BundlesViewModel();
        }

        public Task RefreshWidget()
        {
            return Task.CompletedTask;
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