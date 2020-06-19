using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusCenter : UserControl
    {
        public StatusCenter()
        {
            this.InitializeComponent();
        }
    }

    public class StatusBanner
    {
        public string Title { get; set; }
        public StatusBannerSeverity Severity { get; set; }
        public StatusBannerOperation Operation { get; set; }
        public string Message { get; set; }
        public uint Progress { get; set; } = 0;

        private Color StrokeColor { get; set; }
        private IconSource Glyph { get; set; }

        public enum StatusBannerSeverity
        {
            Ongoing,
            Success,
            Error
        }

        public enum StatusBannerOperation
        {
            Delete,
            Paste,
            Extract
        }

        public StatusBanner()
        {

        }
    }
}
