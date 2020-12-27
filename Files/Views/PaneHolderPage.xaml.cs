using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class PaneHolderPage : Page, IShellPage, INotifyPropertyChanged
    {
        public PaneHolderPage()
        {
            this.InitializeComponent();
            ActivePane = PaneLeft;
        }

        private string navParamsLeft;

        public string NavParamsLeft
        {
            get => navParamsLeft;
            set
            {
                if (navParamsLeft != value)
                {
                    navParamsLeft = value;
                    NotifyPropertyChanged("NavParamsLeft");
                }
            }
        }

        private string navParamsRight;

        public string NavParamsRight
        {
            get => navParamsRight;
            set
            {
                if (navParamsRight != value)
                {
                    navParamsRight = value;
                    NotifyPropertyChanged("NavParamsRight");
                }
            }
        }

        private IShellPage activePane;

        public IShellPage ActivePane
        {
            get => activePane;
            set
            {
                if (activePane != value)
                {
                    activePane = value;
                    NotifyPropertyChanged("ActivePane");
                }
            }
        }

        public StatusBarControl BottomStatusStripControl => ActivePane?.BottomStatusStripControl;

        public Frame ContentFrame => ActivePane?.ContentFrame;

        public Interaction InteractionOperations => ActivePane?.InteractionOperations;

        public ItemViewModel FilesystemViewModel => ActivePane?.FilesystemViewModel;

        public CurrentInstanceViewModel InstanceViewModel => ActivePane?.InstanceViewModel;

        public AppServiceConnection ServiceConnection => ActivePane?.ServiceConnection;

        BaseLayout IShellPage.ContentPage => ActivePane?.ContentPage;

        public Control OperationsControl => ActivePane?.OperationsControl;

        public Type CurrentPageType => ActivePane?.CurrentPageType;

        public INavigationControlItem SidebarSelectedItem
        {
            get => ActivePane?.SidebarSelectedItem;
            set
            {
                if (ActivePane != null)
                {
                    ActivePane.SidebarSelectedItem = value;
                }
            }
        }

        public INavigationToolbar NavigationToolbar => ActivePane?.NavigationToolbar;

        private bool isCurrentInstance;

        public bool IsCurrentInstance
        {
            get => isCurrentInstance;
            set
            {
                isCurrentInstance = value;
                PaneLeft.IsCurrentInstance = false;
                PaneRight.IsCurrentInstance = false;
                if (ActivePane != null)
                {
                    ActivePane.IsCurrentInstance = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParamsLeft = eventArgs.Parameter.ToString();
            NavParamsRight = "NewTab".GetLocalized();
        }

        public void Clipboard_ContentChanged(object sender, object e)
        {
            
        }

        public void Refresh_Click()
        {
            ActivePane?.Refresh_Click();
        }

        public void Dispose()
        {
            PaneLeft.Dispose();
            PaneRight.Dispose();
        }
    }
}
