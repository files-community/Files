//  ---- Settings.xaml.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains code to handle user configurations and use of certain features ---- 
//




using Files.SettingsPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;



namespace Files
{
    
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
            SecondaryPane.SelectedIndex = 0;
            SettingsContentFrame.Navigate(typeof(Personalization));
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            if(item.Name == "Personalization")
            {
                SettingsContentFrame.Navigate(typeof(Personalization));
            }else if(item.Name == "Features")
            {

            }else if(item.Name == "About")
            {

            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(ListViewItem lvi in SecondaryPane.Items)
            {
                
            }
        }
    }
}
