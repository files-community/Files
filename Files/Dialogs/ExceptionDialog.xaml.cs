using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class ExceptionDialog : ContentDialog
    {
        private string message;
        private string stackTrace;
        private string offendingMethod;

        public ExceptionDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (InstanceTabsView.tabView.TabItems.Count == 1)
            {
                Application.Current.Exit();
            }
            else if (InstanceTabsView.tabView.TabItems.Count > 1)
            {
                InstanceTabsView.tabView.TabItems.RemoveAt(InstanceTabsView.tabView.SelectedIndex);
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Application.Current.Exit();
        }

        private void ExpandMoreInfo_Click(object sender, RoutedEventArgs e)
        {
            // If technical info is collapsed
            if (CollapseIcon.Visibility == Visibility.Collapsed)
            {
                ExpandIcon.Visibility = Visibility.Collapsed;
                CollapseIcon.Visibility = Visibility.Visible;
                TechnicalInformation.Visibility = Visibility.Visible;
            }
            else // if technical info is expanded
            {
                ExpandIcon.Visibility = Visibility.Visible;
                CollapseIcon.Visibility = Visibility.Collapsed;
                TechnicalInformation.Visibility = Visibility.Collapsed;
            }
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            message = App.ExceptionInfo.Exception.Message;
            if (!string.IsNullOrWhiteSpace(App.ExceptionStackTrace))
                stackTrace = App.ExceptionStackTrace;
            else
                stackTrace = "No stack trace found.";

            if (!string.IsNullOrWhiteSpace(App.ExceptionInfo.Exception.TargetSite?.ReflectedType.FullName))
                offendingMethod = App.ExceptionInfo.Exception.TargetSite.ReflectedType.FullName;
            else
                offendingMethod = "(Method name unknown)";

            Summary.Text = message + " within method " + offendingMethod;
            ErrorInfo.Text = stackTrace;
        }
    }
}