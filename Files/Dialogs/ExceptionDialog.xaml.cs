using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            if (App.MultitaskingControl.Items.Count == 1)
            {
                App.CloseApp();
            }
            else if (App.MultitaskingControl.Items.Count > 1)
            {
                App.MultitaskingControl.Items.RemoveAt(App.InteractionViewModel.TabStripSelectedIndex);
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            App.CloseApp();
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