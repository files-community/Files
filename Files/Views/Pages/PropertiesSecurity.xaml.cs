using Files.Filesystem;
using Files.Filesystem.Security;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        #region Variables


        #endregion

        #region Constructors

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

        #endregion

        #region Properties



        #endregion

        #region Methods

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// Returns true if properties have been saved successfully.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public override Task<bool> SaveChangesAsync(ListedItem item)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region Events

        protected override async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties != null)
            {
            }

            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
        }

        private void btnEditUserOrGroup_Click(object sender, RoutedEventArgs e)
        {
            
        }

        #endregion

    }
}