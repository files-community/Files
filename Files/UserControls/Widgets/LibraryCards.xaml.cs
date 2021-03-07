using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files.UserControls.Widgets
{
    public sealed partial class LibraryCards : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardInvokedEventHandler LibraryCardInvoked;

        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();

        public LibraryCards()
        {
            InitializeComponent();
            itemsAdded.Clear();
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xE8FC",
                Text = "SidebarDesktop".GetLocalized(),
                Tag = "Desktop"
            });
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xE896",
                Text = "SidebarDownloads".GetLocalized(),
                Tag = "Downloads"
            });
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xE8A5",
                Text = "SidebarDocuments".GetLocalized(),
                Tag = "Documents"
            });
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xEB9F",
                Text = "SidebarPictures".GetLocalized(),
                Tag = "Pictures"
            });
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xEC4F",
                Text = "SidebarMusic".GetLocalized(),
                Tag = "Music"
            });
            itemsAdded.Add(new FavoriteLocationItem()
            {
                Icon = "\xE8B2",
                Text = "SidebarVideos".GetLocalized(),
                Tag = "Videos"
            });
            foreach (var item in itemsAdded)
            {
                item.AutomationProperties = item.Text;
            }
        }

        private void GridScaleUp(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Source for the scaling: https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.Uwp.SampleApp/SamplePages/Implicit%20Animations/ImplicitAnimationsPage.xaml.cs
            // Search for "Scale Element".
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1.02f, 1.02f, 1);
        }

        private void GridScaleNormal(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            switch (ClickedCard)
            {
                case "Desktop":
                    NavigationPath = AppSettings.DesktopPath;
                    break;

                case "Downloads":
                    NavigationPath = AppSettings.DownloadsPath;
                    break;

                case "Documents":
                    NavigationPath = AppSettings.DocumentsPath;
                    break;

                case "Pictures":
                    NavigationPath = AppSettings.PicturesPath;
                    break;

                case "Music":
                    NavigationPath = AppSettings.MusicPath;
                    break;

                case "Videos":
                    NavigationPath = AppSettings.VideosPath;
                    break;
            }
            LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs()
            {
                Path = NavigationPath
            });
        }
    }

    public class LibraryCardInvokedEventArgs : EventArgs
    {
        public string Path { get; set; }
    }

    public class FavoriteLocationItem
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
        public string AutomationProperties { get; set; }
    }
}