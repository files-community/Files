using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Files.App.UITests.Dialogs;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.UITests.Views
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DialogWindowsPage : Page
    {


        public DialogWindowsPage()
        {
            InitializeComponent();
        }

        private void BtnProperties_Click(object sender , RoutedEventArgs e)
        {
            PropertiesDialog propWindow = new PropertiesDialog();
;
            if ( propWindow != null )
            {
                propWindow.Title = "Properties";
                propWindow.Activate();
            }
        }
    }
}
