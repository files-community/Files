using Files.DataModels;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using static Files.UserControls.INavigationToolbar;

namespace Files.UserControls
{
    public sealed partial class NavigationToolbar : UserControl
    {
        // TODO: Remove this MainPage reference when we work on new Vertical Tabs control in MainPage
        private MainPage mainPage => ((Window.Current.Content as Frame).Content as MainPage);

        public NavToolbarViewModel ViewModel
        {
            get => (NavToolbarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(NavToolbarViewModel), typeof(NavigationToolbar), new PropertyMetadata(null));

        public ISearchBox SearchBox => ViewModel.SearchBox;

        public MainViewModel MainViewModel => App.MainViewModel;


        public static readonly DependencyProperty CanPasteInPageProperty = DependencyProperty.Register(
          "CanPasteInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanPasteInPage
        {
            get
            {
                return (bool)GetValue(CanPasteInPageProperty);
            }
            set
            {
                SetValue(CanPasteInPageProperty, value);
            }
        }

        public SettingsViewModel AppSettings => App.AppSettings;

        private List<ShellNewEntry> cachedNewContextMenuEntries { get; set; }


        public NavigationToolbar()
        {
            InitializeComponent();
            Loading += NavigationToolbar_Loading;
        }

        private async void NavigationToolbar_Loading(FrameworkElement sender, object args)
        {
            cachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();
        }


        public bool ShowMultiPaneControls
        {
            get => (bool)GetValue(ShowMultiPaneControlsProperty);
            set => SetValue(ShowMultiPaneControlsProperty, value);
        }

        // Using a DependencyProperty as the backing store for ShowMultiPaneControls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowMultiPaneControlsProperty =
            DependencyProperty.Register(nameof(ShowMultiPaneControls), typeof(bool), typeof(NavigationToolbar), new PropertyMetadata(null));

        private bool isMultiPaneActive;

        public bool IsMultiPaneActive
        {
            get
            {
                return isMultiPaneActive;
            }
            set
            {
                if (value != isMultiPaneActive)
                {
                    isMultiPaneActive = value;
                }
            }
        }

        private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
        {
            // AutoSuggestBox won't receive focus unless it's fully loaded
            VisiblePath.Focus(FocusState.Programmatic);
            DependencyObjectHelpers.FindChild<TextBox>(VisiblePath)?.SelectAll();
        }

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEditModeEnabled = true;
        }

        private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                ViewModel.IsEditModeEnabled = false;
            }
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase ||
                FocusManager.GetFocusedElement() is AppBarButton ||
                FocusManager.GetFocusedElement() is Popup)
            {
                return;
            }

            var element = FocusManager.GetFocusedElement();
            var elementAsControl = element as Control;
            if (elementAsControl == null)
            {
                return;
            }
            else if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
            {
                ViewModel.IsEditModeEnabled = false;
            }
            else
            {
                if (ViewModel.IsEditModeEnabled)
                {
                    this.VisiblePath.Focus(FocusState.Programmatic);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchSearchBoxVisibility();

        private void SearchBox_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.CloseSearchBox();

        private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_LostFocus(sender, e);

        private void NavMoreButtonFlyout_Opening(object sender, object e)
        {
            if (cachedNewContextMenuEntries == null)
            {
                return;
            }
            if (!NewEmptySpace.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
            {
                var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));
                foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
                {
                    MenuFlyoutItem menuLayoutItem;
                    if (newEntry.Icon != null)
                    {
                        BitmapImage image = null;
                        image = new BitmapImage();
#pragma warning disable CS4014
                        image.SetSourceAsync(newEntry.Icon);
#pragma warning restore CS4014
                        menuLayoutItem = new MenuFlyoutItemWithImage()
                        {
                            Text = newEntry.Name,
                            BitmapIcon = image,
                            Tag = "CreateNewFile"
                        };
                    }
                    else
                    {
                        menuLayoutItem = new MenuFlyoutItem()
                        {
                            Text = newEntry.Name,
                            Icon = new FontIcon()
                            {
                                Glyph = "\xE7C3"
                            },
                            Tag = "CreateNewFile"
                        };
                    }
                    menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
                    menuLayoutItem.CommandParameter = newEntry;
                    NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
                }
            }
        }

        private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ViewModel.VisiblePath_QuerySubmitted(sender, args);
    }
}