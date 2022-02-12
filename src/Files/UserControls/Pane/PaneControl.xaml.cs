using Files.Enums;
using Files.Services;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class PaneControl : UserControl
    {
        private IPaneSettingsService PaneService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        private Control pane;

        public PanePosition Position { get; }

        public PaneControl()
        {
            InitializeComponent();

            PaneService.PropertyChanged += PaneService_PropertyChanged;
        }

        private void PaneService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPaneSettingsService.Content))
            {
                pane = GetPane();

                Panel.Children.Clear();
                if (pane is not null)
                {
                    Panel.Children.Add(pane);
                }
            }
        }

        private Control GetPane() => PaneService.Content switch
        {
            PaneContents.Preview => new PreviewPane(),
            _ => null,
        };
    }
}
