using Files.Common;
using Files.ViewModels.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PropertiesCustomization : PropertiesTab
    {
        public PropertiesCustomization()
        {
            this.InitializeComponent();
        }

        private async void PropertiesChangeFolderIconButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newWindow = CoreApplication.CreateNewView();
            ApplicationView newView = null;

            var response = await AppInstance?.ServiceConnection?.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "GetFolderIconsFromDLL" },
                { "iconFile", @"C:\Windows\System32\SHELL32.dll" }
            });

            var icons = JsonConvert.DeserializeObject<IList<IconFileInfo>>(response.Data["IconInfos"] as string);

            await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(CustomFolderIcons), icons, new SuppressNavigationTransitionInfo());
                Window.Current.Content = frame;
                Window.Current.Activate();

                newView = ApplicationView.GetForCurrentView();
                newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                // TODO: Localize this title
                newView.Title = "Selecting custom icon for " + ViewModel.ItemName;
                newView.PersistedStateId = "CustomIcon";
                newView.SetPreferredMinSize(new Size(400, 550));
                newView.Consolidated += delegate
                {
                    Window.Current.Close();
                };
            });

            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newView.Id);
            // Set window size again here as sometimes it's not resized in the page Loaded event
            newView.TryResizeView(new Size(400, 550));
        }
    }
}
