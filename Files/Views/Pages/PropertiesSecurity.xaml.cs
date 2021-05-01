using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
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

            List<Views.Permission> permissions = new List<Views.Permission>();
            permissions.Add(new Views.Permission() { Id = 1, Descripton = "Full Control", Allow = true, Deny = false });
            permissions.Add(new Views.Permission() { Id = 2, Descripton = "Modify", Allow = true, Deny = false });
            permissions.Add(new Views.Permission() { Id = 3, Descripton = "Read & execute", Allow = true, Deny = false });
            permissions.Add(new Views.Permission() { Id = 4, Descripton = "Read", Allow = true, Deny = false });
            permissions.Add(new Views.Permission() { Id = 5, Descripton = "Write", Allow = true, Deny = false });
            permissions.Add(new Views.Permission() { Id = 6, Descripton = "Special permissions", Allow = true, Deny = false });

            lstPermissions.ItemsSource = permissions;

            List<Views.UserGroups> usergroups = new List<Views.UserGroups>();
            usergroups.Add(new Views.UserGroups() { Id = 1, Icon = "&#xE77B;", Description = "SYSTEM", Path = " (system) ", ItemType = SecurityType.User });
            usergroups.Add(new Views.UserGroups() { Id = 2, Icon = "&#xE716;", Description = "UserTest", Path = " (usertest@test) ", ItemType = SecurityType.Group });
            usergroups.Add(new Views.UserGroups() { Id = 3, Icon = "&#xE77B;", Description = "Administrators", Path = " (administrators\\administrator) ", ItemType = SecurityType.User });

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

    public class Permission
    {
        public int Id { get; set; }
        public string Descripton { get; set; }
        public bool Allow { get; set; }
        public bool Deny { get; set; }
    }

    public class UserGroups
    {
        public int Id { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public SecurityType ItemType { get; set; }
    }
    public enum SecurityType { User, Group };

}