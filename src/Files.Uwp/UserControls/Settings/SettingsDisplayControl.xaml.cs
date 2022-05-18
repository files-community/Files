using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Files.Uwp.UserControls.Settings
{
    [ContentProperty(Name = nameof(SettingsActionableElement))]
    public sealed partial class SettingsDisplayControl : UserControl
    {
        public FrameworkElement SettingsActionableElement { get; set; }

        public static readonly DependencyProperty AdditionalDescriptionContentProperty = DependencyProperty.Register(
          "AdditionalDescriptionContent",
          typeof(FrameworkElement),
          typeof(SettingsDisplayControl),
          new PropertyMetadata(null)
        );

        public FrameworkElement AdditionalDescriptionContent
        {
            get => (FrameworkElement)GetValue(AdditionalDescriptionContentProperty);
            set => SetValue(AdditionalDescriptionContentProperty, value);
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
          "Title",
          typeof(string),
          typeof(SettingsDisplayControl),
          new PropertyMetadata(null)
        );

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
          "Description",
          typeof(string),
          typeof(SettingsDisplayControl),
          new PropertyMetadata(null)
        );

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
          "Icon",
          typeof(IconElement),
          typeof(SettingsDisplayControl),
          new PropertyMetadata(null)
        );

        public IconElement Icon
        {
            get => (IconElement)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public SettingsDisplayControl()
        {
            this.InitializeComponent();
            VisualStateManager.GoToState(this, "NormalState", false);
        }

        private void MainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == e.PreviousSize.Width || ActionableElement == null)
                return;

            if (ActionableElement.ActualWidth > e.NewSize.Width / 3)
            {
                VisualStateManager.GoToState(this, "CompactState", false);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalState", false);
            }
        }
    }
}