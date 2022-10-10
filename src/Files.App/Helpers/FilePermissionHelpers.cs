#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Files.App.Filesystem.Permissions;
using Files.App.Shell;
using Tulpep.ActiveDirectoryObjectPicker;
using AccessControlType = System.Security.AccessControl.AccessControlType;
using FileSystemAccessRule = System.Security.AccessControl.FileSystemAccessRule;
using FileSystemRights = System.Security.AccessControl.FileSystemRights;
using InheritanceFlags = System.Security.AccessControl.InheritanceFlags;
using PropagationFlags = System.Security.AccessControl.PropagationFlags;

namespace Files.App.Helpers;

public static class FilePermissionHelpers
{
    public static bool GetAccessControl(string filePath, bool isFolder, out FileSystemSecurity? fss)
    {
        try
        {
            if (isFolder && Directory.Exists(filePath))
            {
                fss = new DirectoryInfo(filePath).GetAccessControl();
                return true;
            }

            if (File.Exists(filePath))
            {
                fss = new FileInfo(filePath).GetAccessControl();
                return true;
            }

            // File or folder does not exists
            fss = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // User does not have rights to read access rules
            fss = null;
            return false;
        }
        catch (Exception)
        {
            fss = null;
            return false;
        }
    }

    public static bool SetOwner(this FilePermissionsManager filePermissions, string ownerSid)
    {
        var acsResult = GetAccessControl(filePermissions.FilePath, filePermissions.IsFolder, out FileSystemSecurity? acs);

        if (!acsResult)
            return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{filePermissions.FilePath}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{ownerSid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);

        if (acs is null)
        {
            App.Logger.Error("acs was null");
            return false;
        }

        try
        {
            acs.SetOwner(new SecurityIdentifier(ownerSid));

            if (filePermissions.IsFolder)
            {
                var di = new DirectoryInfo(filePermissions.FilePath);
                di.SetAccessControl((DirectorySecurity)acs);
            }
            else
            {
                var fi = new FileInfo(filePermissions.FilePath);
                fi.SetAccessControl((FileSecurity)acs);
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // User does not have rights to set the owner
        }
        catch (Exception)
        {
            // ignored
        }

        // Set through powershell (admin)
        return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{filePermissions.FilePath}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{ownerSid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
    }

    public static bool SetAccessRuleProtection(this FilePermissionsManager filePermissions, bool isProtected, bool preserveInheritance)
    {
        var acsResult = GetAccessControl(filePermissions.FilePath, filePermissions.IsFolder, out var acs);

        if (!acsResult)
            return false;

        if (acs is null)
        {
            App.Logger.Error("acs was null");
            return false;
        }

        try
        {
            acs.SetAccessRuleProtection(isProtected, preserveInheritance);

            if (filePermissions.IsFolder)
            {
                var di = new DirectoryInfo(filePermissions.FilePath);
                di.SetAccessControl((DirectorySecurity)acs);
            }
            else
            {
                var fi = new FileInfo(filePermissions.FilePath);
                fi.SetAccessControl((FileSecurity)acs);
            }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // User does not have rights to set access rules
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool SetPermissions(this FilePermissionsManager filePermissions)
    {
        var acsResult = GetAccessControl(filePermissions.FilePath, filePermissions.IsFolder, out var acs);

        if (!acsResult)
            return false;

        if (acs is null)
        {
            App.Logger.Error("acs was null");
            return false;
        }

        try
        {
            var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (var existingRule in accessRules.Cast<FileSystemAccessRule>().Where(x => !x.IsInherited))
            {
                acs.RemoveAccessRule(existingRule);
            }
            foreach (var rule in filePermissions.AccessRules.Where(x => !x.IsInherited))
            {
                var acsRule = new FileSystemAccessRule(
                    new SecurityIdentifier(rule.IdentityReference),
                    (FileSystemRights)(int)rule.FileSystemRights,
                    (InheritanceFlags)(int)rule.InheritanceFlags,
                    (PropagationFlags)(int)rule.PropagationFlags,
                    (AccessControlType)(int)rule.AccessControlType
                );
                acs.AddAccessRule(acsRule);
            }
            if (filePermissions.IsFolder)
            {
                var di = new DirectoryInfo(filePermissions.FilePath);
                di.SetAccessControl((DirectorySecurity)acs);
            }
            else
            {
                var fi = new FileInfo(filePermissions.FilePath);
                fi.SetAccessControl((FileSecurity)acs);
            }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // User does not have rights to set access rules
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static Task<string?> OpenObjectPicker(long hWnd)
    {
        return Win32API.StartSTATask(() =>
        {
            DirectoryObjectPickerDialog picker = new DirectoryObjectPickerDialog()
            {
                AllowedObjectTypes = ObjectTypes.All,
                DefaultObjectTypes = ObjectTypes.Users | ObjectTypes.Groups,
                AllowedLocations = Locations.All,
                DefaultLocations = Locations.LocalComputer,
                MultiSelect = false,
                ShowAdvancedView = true
            };
            picker.AttributesToFetch.Add("objectSid");
            using (picker)
            {
                if (picker.ShowDialog(Win32API.Win32Window.FromLong(hWnd)) != DialogResult.OK)
                    return null;

                try
                {
                    var attributes = picker.SelectedObject.FetchedAttributes;
                    if (attributes.Any() && attributes[0] is byte[] objectSid)
                        return new SecurityIdentifier(objectSid, 0).Value;
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, ex.Message);
                }
            }
            return null;
        });
    }

    public static FilePermissions? FromFilePath(string filePath, bool isFolder)
    {
        var filePermissions = new FilePermissions { FilePath = filePath, IsFolder = isFolder };
        var acsResult = GetAccessControl(filePath, isFolder, out var acs);

        if (acsResult)
        {
            if (acs is null)
            {
                App.Logger.Error("acs was null");
                return null;
            }

            var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
            filePermissions.OwnerSID = acs.GetOwner(typeof(SecurityIdentifier))!.Value;
            filePermissions.AreAccessRulesProtected = acs.AreAccessRulesProtected;

            foreach (System.Security.AccessControl.FileSystemAccessRule accessRule in accessRules)
            {
                filePermissions.AccessRules.Add(new Filesystem.Permissions.FileSystemAccessRule
                {
                    AccessControlType = (Filesystem.Permissions.AccessControlType)(int)accessRule.AccessControlType,
                    InheritanceFlags = (Filesystem.Permissions.InheritanceFlags)(int)accessRule.InheritanceFlags,
                    FileSystemRights = (Filesystem.Permissions.FileSystemRights)(int)accessRule.FileSystemRights,
                    PropagationFlags = (Filesystem.Permissions.PropagationFlags)(int)accessRule.PropagationFlags,
                    IdentityReference = accessRule.IdentityReference.Value,
                    IsInherited = accessRule.IsInherited,
                });
            }
        }
        filePermissions.CanReadFilePermissions = acsResult;
        return filePermissions;
    }
}
