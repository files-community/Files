using Files.ViewModels.Search;
using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls.Search
{
    public sealed partial class TagsControl : UserControl
    {
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register(nameof(Tags), typeof(IEnumerable<ISearchTagViewModel>), typeof(TagsControl), new PropertyMetadata(null));

        public static readonly DependencyProperty MiddleFilterProperty =
            DependencyProperty.Register(nameof(MiddleFilter), typeof(ISearchFilterViewModel), typeof(TagsControl), new PropertyMetadata(null));


        public static readonly DependencyProperty CanSelectProperty =
            DependencyProperty.Register(nameof(CanSelect), typeof(bool), typeof(TagsControl), new PropertyMetadata(false));

        public static readonly DependencyProperty CanCloseProperty =
            DependencyProperty.Register(nameof(CanClose), typeof(bool), typeof(TagsControl), new PropertyMetadata(false));

        public IEnumerable<ISearchTagViewModel> Tags
        {
            get => (IEnumerable<ISearchTagViewModel>)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        public ISearchFilterViewModel MiddleFilter
        {
            get => (ISearchFilterViewModel)GetValue(MiddleFilterProperty);
            set => SetValue(MiddleFilterProperty, value);
        }

        public bool CanSelect
        {
            get => (bool)GetValue(CanSelectProperty);
            set => SetValue(CanSelectProperty, value);
        }

        public bool CanClose
        {
            get => (bool)GetValue(CanCloseProperty);
            set => SetValue(CanCloseProperty, value);
        }

        public TagsControl() => InitializeComponent();

        private void MainButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (CanClose && sender is FrameworkElement element)
            {
                var button = element.FindDescendant("CloseButton") as Button;
                if (button is not null)
                {
                    button.Visibility = Visibility.Visible;
                }
            }
        }
        private void MainButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var button = element.FindDescendant("CloseButton") as Button;
                if (button is not null)
                {
                    button.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void MainButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CanSelect && sender is Button button)
            {
                if (button.DataContext is ISearchTagViewModel tag)
                {
                    tag.Select(MiddleFilter);
                }
            }
        }
        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CanClose && sender is Button button)
            {
                if (button.DataContext is ISearchTagViewModel tag)
                {
                    tag.Close();
                }
            }
        }
    }
}
