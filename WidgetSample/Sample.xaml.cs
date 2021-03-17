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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SampleWidget
{
    public sealed partial class Sample : UserControl
    {
        public Sample()
        {
            Uri resourceLocator = new Uri("ms-appx://6889f09e-8508-4cce-8819-546f904e48b0/Sample.xaml");
            Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
        }
    }
}
