using Files.Views;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public interface IRibbonToolbar
    {
        public bool CanCopy { get; set; }
        public bool CanPaste { get; set; }
        public bool CanCut { get; set; }

        public event EventHandler CopyRequested;

        public event EventHandler PasteRequested;

        public event EventHandler CutRequested;
    }
}