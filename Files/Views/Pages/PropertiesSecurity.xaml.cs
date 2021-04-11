using Files.Dialogs;
using Files.Enums;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public PropertiesSecurity()
        {
            InitializeComponent();

            List<User> groupsUsersItems = new List<User>();
            List<Permission> permissionItems = new List<Permission>();

            lvGroupsOrUsers.ItemsSource = groupsUsersItems;
            lvPermissions.ItemsSource = permissionItems;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                (BaseProperties as FileProperties).GetSecurityProperties();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <returns>Returns true if properties have been saved successfully.</returns>
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
                    if (UIHelpers.IsAnyContentDialogOpen())
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

        public class User
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public string Mail { get; set; }
        }

        public class Permission
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public string Mail { get; set; }
        }

        private void ItemPermissionsDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ItemAdvanPermissionsDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}