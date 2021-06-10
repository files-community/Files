using Files.Common;
using Files.Filesystem;
using Files.Filesystem.Permissions;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.UI.Core;

namespace Files.ViewModels.Properties
{
    public class FileSystemProperties : BaseProperties
    {
        public ListedItem Item { get; }

        public FileSystemProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ListedItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Item = item;
            AppInstance = instance;

            GetBaseProperties();

            ViewModel.EditOwnerCommand = new RelayCommand(() => EditOwner(), () => ViewModel.FilePermissions != null);
            ViewModel.AddRulesForUserCommand = new RelayCommand(() => AddRulesForUser(), () => ViewModel.FilePermissions != null && ViewModel.FilePermissions.CanReadFilePermissions);
            ViewModel.RemoveRulesForUserCommand = new RelayCommand(() => RemoveRulesForUser(), () => ViewModel.FilePermissions != null && ViewModel.FilePermissions.CanReadFilePermissions && ViewModel.SelectedRuleForUser != null);
        }

        private async void EditOwner()
        {
            var pickedObject = await OpenObjectPicker();
            if (pickedObject != null)
            {
                if (await SetFileOwner(pickedObject))
                {
                    GetFilePermissions(); // Refresh file permissions
                }
            }
        }

        private async void AddRulesForUser()
        {
            var pickedObject = await OpenObjectPicker();
            if (pickedObject != null)
            {
                if (!ViewModel.FilePermissions.RulesForUsers.Any(x => x.UserGroup.Sid == pickedObject))
                {
                    // No existing rules, add user to list
                    ViewModel.FilePermissions.RulesForUsers.Add(RulesForUser.ForUser(ViewModel.FilePermissions.AccessRules, ViewModel.IsFolder, pickedObject));
                }
            }
        }

        private void RemoveRulesForUser()
        {
            if (ViewModel.SelectedRuleForUser != null)
            {
                ViewModel.SelectedRuleForUser.AllowRights = 0;
                ViewModel.SelectedRuleForUser.DenyRights = 0;

                if (!ViewModel.FilePermissions.AccessRules.Any(x => x.IdentityReference == ViewModel.SelectedRuleForUser.UserGroup.Sid))
                {
                    // No remaining rules, remove user from list
                    ViewModel.FilePermissions.RulesForUsers.Remove(ViewModel.SelectedRuleForUser);
                }
            }
        }

        public override void GetBaseProperties()
        {            
        }

        public override void GetSpecialProperties()
        {            
        }

        public async void GetFilePermissions()
        {
            if (AppInstance.ServiceConnection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "GetFilePermissions" },
                    { "filepath", Item.ItemPath },
                    { "isfolder", Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcutItem }
                };
                var (status, response) = await AppInstance.ServiceConnection.SendMessageForResponseAsync(value);
                if (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                {
                    var filePermissions = JsonConvert.DeserializeObject<FilePermissions>((string)response["FilePermissions"]);
                    ViewModel.FilePermissions = new FilePermissionsManager(filePermissions);
                }
            }
        }

        public async Task<bool> SetFilePermissions()
        {
            if (ViewModel.FilePermissions == null || !ViewModel.FilePermissions.CanReadFilePermissions)
            {
                return true;
            }
            if (AppInstance.ServiceConnection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "SetFilePermissions" },
                    { "permissions", JsonConvert.SerializeObject(ViewModel.FilePermissions.ToFilePermissions()) }
                };
                var (status, response) = await AppInstance.ServiceConnection.SendMessageForResponseAsync(value);
                return (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                    && response.Get("Success", false));
            }
            return false;
        }

        public async Task<bool> SetFileOwner(string ownerSid)
        {
            if (AppInstance.ServiceConnection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "SetFileOwner" },
                    { "filepath", Item.ItemPath },
                    { "isfolder", Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcutItem },
                    { "ownersid", ownerSid }
                };
                var (status, response) = await AppInstance.ServiceConnection.SendMessageForResponseAsync(value);
                return (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                    && response.Get("Success", false));
            }
            return false;
        }

        public async Task<string> OpenObjectPicker()
        {
            if (AppInstance.ServiceConnection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "OpenObjectPicker" },
                    { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
                };
                var (status, response) = await AppInstance.ServiceConnection.SendMessageForResponseAsync(value);
                if (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                {
                    return response.Get("PickedObject", (string)null);
                }
            }
            return null;
        }
    }
}
