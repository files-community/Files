using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class PaneControl : UserControl, IPane
    {
        public event EventHandler<PaneControlUpdatedEventArgs> Updated;

        public PanePositions Position { get; private set; } = PanePositions.None;

        public PaneContents Content { get; private set; }

        public Control Pane { get; private set; }

        private IPaneSettingsService PaneService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        public PaneControl()
        {
            InitializeComponent();

            PaneService.PropertyChanged += PaneService_PropertyChanged;
        }

        public void UpdatePosition(double panelWidth, double panelHeight)
        {
            if (Pane is IPane p)
            {
                p.UpdatePosition(panelWidth, panelHeight);
                Position = p.Position;
            }
            else
            {
                Position = Pane is not null ? PanePositions.Right : PanePositions.None;
            }

            if (Pane is not null)
            {
                MinWidth = Pane.MinWidth;
                MaxWidth = Pane.MaxWidth;
                MinHeight = Pane.MinHeight;
                MaxHeight = Pane.MaxHeight;
            }
        }

        private void PaneService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPaneSettingsService.Content))
            {
                Content = PaneService.Content;
                if (Content is PaneContents.None)
                {
                    Updated?.Invoke(this, new PaneControlUpdatedEventArgs(Content, Pane));
                }
            }
        }


        private void Pane_Loading(FrameworkElement sender, object args)
        {
            Pane = sender as Control;
            Updated?.Invoke(this, new PaneControlUpdatedEventArgs(Content, Pane));
        }
    }

    public class PaneControlUpdatedEventArgs : EventArgs
    {
        public Control Pane { get; }
        public PaneContents Content { get; }

        public PaneControlUpdatedEventArgs(PaneContents content, Control pane)
            => (Content, Pane) = (content, pane);
    }

    public class PaneTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PreviewTemplate { get; set; }
        public DataTemplate SearchTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            PaneContents.Preview => PreviewTemplate,
            PaneContents.Search => SearchTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
