using Files.View_Models.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Dialogs;

namespace Files
{
    public sealed partial class PropertiesDetails : PropertiesTab
    {
        public PropertiesDetails()
        {
            InitializeComponent();

            // For some reason, binding the converter XAML Markup would throw a COM error
            // To work around this, the item template is defined here   
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                (BaseProperties as FileProperties).GetSystemFileProperties();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Returns false if the operation was cancelled
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                await (BaseProperties as FileProperties).SyncPropertyChangesAsync();
            }
            catch (Exception error)
            {
                var dialog = new PropertySaveError();

                switch (await dialog.ShowAsync())
                {
                    case ContentDialogResult.Primary:
                        SaveChangesAsync();
                        return false;

                    case ContentDialogResult.Secondary:
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private async void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
        {
            ClearPropertiesFlyout.Hide();
            await (BaseProperties as FileProperties).ClearPropertiesAsync();
        }
    }
}
