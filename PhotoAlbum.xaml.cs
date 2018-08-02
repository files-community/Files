//  ---- PhotoAlbum.xaml.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains various behind-the-scenes code for the image-optimized layout ---- 
//

using ItemListPresenter;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;



namespace Files
{

    public sealed partial class PhotoAlbum : Page
    {
        public static GridView gv;
        public static Image largeImg;
        public static MenuFlyout context;

        public PhotoAlbum()
        {
            this.InitializeComponent();
            
            gv = FileList;
            context = RightClickContextMenu;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter.ToString();
            ItemViewModel.ViewModel = new ItemViewModel(parameters, true);
            Interact.Interaction.page = this;
            FileList.ItemClick += Interact.Interaction.PhotoAlbumItemList_ClickAsync;
            Back.Click += Navigation.PhotoAlbumNavActions.Back_Click;
            Forward.Click += Navigation.PhotoAlbumNavActions.Forward_Click;
            Refresh.Click += Navigation.PhotoAlbumNavActions.Refresh_Click;
            FileList.RightTapped += Interact.Interaction.FileList_RightTapped;
        }

        
    }
    
    


}
