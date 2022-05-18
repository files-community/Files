using Files.Shared.Enums;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.UserControls
{
    public sealed partial class PaneControl : UserControl, IPane
    {
        public PanePositions Position { get; private set; } = PanePositions.None;

        private IPaneSettingsService PaneService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        private PaneContents content;
        private Control pane;

        public PaneControl()
        {
            InitializeComponent();

            PaneService.PropertyChanged += PaneService_PropertyChanged;
            Update();
        }

        public void UpdatePosition(double panelWidth, double panelHeight)
        {
            if (pane is IPane p)
            {
                p.UpdatePosition(panelWidth, panelHeight);
                Position = p.Position;
            }
            if (pane is not null)
            {
                MinWidth = pane.MinWidth;
                MaxWidth = pane.MaxWidth;
                MinHeight = pane.MinHeight;
                MaxHeight = pane.MaxHeight;
            }
        }

        private void PaneService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPaneSettingsService.Content))
            {
                Update();
            }
        }

        private void Update()
        {
            var newContent = PaneService.Content;
            if (content != newContent)
            {
                content = newContent;
                pane = GetPane(content);

                Panel.Children.Clear();
                if (pane is not null)
                {
                    Panel.Children.Add(pane);
                }
            }
        }

        private static Control GetPane(PaneContents content) => content switch
        {
            PaneContents.Preview => new PreviewPane(),
            _ => null,
        };
    }
}
