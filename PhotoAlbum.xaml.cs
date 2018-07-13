using ItemListPresenter;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;



namespace Files
{
    
    public sealed partial class PhotoAlbum : Page
    {
        public static ListView lv;
        public static Image largeImg;
        public PhotoAlbum()
        {
            this.InitializeComponent();
            
            largeImg = LargeDisplayedImage;
            lv = FileList;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter.ToString();
            ItemViewModel.ViewModel = new ItemViewModel(parameters, true);
            Interact.Interaction.page = this;
            FileList.ItemClick += Interact.Interaction.PhotoAlbumItemList_Click;
        }

        
    }
    
    


}
