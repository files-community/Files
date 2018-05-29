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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class YourHome : Page
    {
        public YourHome()
        {
            this.InitializeComponent();
        }

        private void b0_Click(object sender, RoutedEventArgs e)
        {
            //  this.Frame.Navigate(typeof(Desktop));
        }

        private void b1_Click(object sender, RoutedEventArgs e)
        {
            //  this.Frame.Navigate(typeof(Downloads));

        }

        private void b2_Click(object sender, RoutedEventArgs e)
        {
            //  this.Frame.Navigate(typeof(Documents));
        }

        private void b3_Click(object sender, RoutedEventArgs e)
        {
            //  this.Frame.Navigate(typeof(Pictures));
        }

        private void b4_Click(object sender, RoutedEventArgs e)
        {
            //   this.Frame.Navigate(typeof(Music));
        }

        private void b5_Click(object sender, RoutedEventArgs e)
        {
            // this.Frame.Navigate(typeof(Videos));
        }
    }
}
