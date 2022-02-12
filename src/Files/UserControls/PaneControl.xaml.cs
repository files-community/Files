using Files.Enums;
using Files.Services;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class PaneControl : UserControl
    {
        private IPaneSettingsService PaneService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        public PaneControl() => InitializeComponent();

        private void PreviewPane_Loading(FrameworkElement sender, object args)
        {
            var pane = sender as PreviewPane;
            if (pane != null)
            {
                pane.Model = App.PreviewPaneViewModel;
                pane.Model?.UpdateSelectedItemPreview();
            }
        }
    }

    public class PaneTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PreviewTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            PaneContents.Preview => PreviewTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
