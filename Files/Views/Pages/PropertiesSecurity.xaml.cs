using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.Secutiry;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public PropertiesSecurity()
        {
            InitializeComponent();

            List<Permission> permissions = new List<Permission>();
            permissions.Add(new Permission() { Id = 1, Descripton = "Full Control", Allow = true, Deny = false });
            permissions.Add(new Permission() { Id = 2, Descripton = "Modify", Allow = true, Deny = false });
            permissions.Add(new Permission() { Id = 3, Descripton = "Read & execute", Allow = true, Deny = false });
            permissions.Add(new Permission() { Id = 4, Descripton = "Read", Allow = true, Deny = false });
            permissions.Add(new Permission() { Id = 5, Descripton = "Write", Allow = true, Deny = false });
            permissions.Add(new Permission() { Id = 6, Descripton = "Special permissions", Allow = true, Deny = false });

            lstPermissions.ItemsSource = permissions;

            List<UserGroups> usergroups = new List<UserGroups>();
            usergroups.Add(new UserGroups() { Id = 1, Icon = "&#xE77B;", Description = "SYSTEM", Path = " (system) ", ItemType = SecurityType.User });
            usergroups.Add(new UserGroups() { Id = 2, Icon = "&#xE716;", Description = "UserTest", Path = " (usertest@test) ", ItemType = SecurityType.Group });
            usergroups.Add(new UserGroups() { Id = 3, Icon = "&#xE77B;", Description = "Administrators", Path = " (administrators\\administrator) ", ItemType = SecurityType.User });

            lstUserGroups.ItemsSource = usergroups;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties != null)
            {
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
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

        public override Task<bool> SaveChangesAsync(ListedItem item)
        {
            throw new NotImplementedException();
        }
    }
}