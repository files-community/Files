using Files.Dialogs;
using Files.Enums;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Views
{
    public sealed partial class PropertiesHashes : PropertiesTab
    {
        public PropertiesHashes()
        {
            InitializeComponent();
            base.ItemMD5HashProgress = ItemMD5HashProgress;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if ((BaseProperties as FileProperties) != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                (BaseProperties as FileProperties).GetSystemFileHashes();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            while (true)
            {
                using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
                try
                {
                    await (BaseProperties as FileProperties).SyncPropertyChangesAsync();
                    return true;
                }
                catch
                {
                    // Attempting to open more than one ContentDialog
                    // at a time will throw an error)
                    if (Interacts.Interaction.IsAnyContentDialogOpen())
                    {
                        return false;
                    }
                    await dialog.ShowAsync();
                    switch (dialog.DynamicResult)
                    {
                        case DynamicDialogResult.Primary:
                            break;

                        case DynamicDialogResult.Secondary:
                            return true;

                        case DynamicDialogResult.Cancel:
                            return false;
                    }
                }
            }
        }
    }
}