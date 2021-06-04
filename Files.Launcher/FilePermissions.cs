using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tulpep.ActiveDirectoryObjectPicker;

namespace FilesFullTrust
{
    public class FilePermissions
    {
        public string FilePath { get; set; }
        public bool IsFolder { get; set; }

        public bool CanReadFilePermissions { get; set; }

        public string OwnerSID { get; set; }

        public string CurrentUserSID { get; set; }

        private FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule2>();
        }

        public List<FileSystemAccessRule2> AccessRules { get; set; }

        public static FilePermissions FromFilePath(string filePath, bool isFolder)
        {
            var filePermissions = new FilePermissions() { FilePath = filePath, IsFolder = isFolder };
            var acsResult = GetAccessControl(filePath, isFolder, out var acs);
            if (acsResult)
            {
                var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
                filePermissions.AccessRules.AddRange(accessRules.Cast<FileSystemAccessRule>().Select(x => FileSystemAccessRule2.FromFileSystemAccessRule(x)));
                filePermissions.OwnerSID = acs.GetOwner(typeof(SecurityIdentifier)).Value;
            }
            filePermissions.CanReadFilePermissions = acsResult;
            return filePermissions;
        }

        public bool SetPermissions()
        {
            var acsResult = GetAccessControl(FilePath, IsFolder, out var acs);
            if (acsResult)
            {
                try
                {
                    var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
                    foreach (var existingRule in accessRules.Cast<FileSystemAccessRule>().Where(x => !x.IsInherited))
                    {
                        acs.RemoveAccessRule(existingRule);
                    }
                    foreach (var rule in AccessRules.Where(x => !x.IsInherited))
                    {
                        acs.AddAccessRule(rule.ToFileSystemAccessRule());
                    }
                    if (IsFolder)
                    {
                        Directory.SetAccessControl(FilePath, acs as DirectorySecurity);
                    }
                    else
                    {
                        File.SetAccessControl(FilePath, acs as FileSecurity);
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
            return false;
        }

        public bool SetOwner(string ownerSid)
        {
            var acsResult = GetAccessControl(FilePath, IsFolder, out var acs);
            if (acsResult)
            {
                try
                {
                    acs.SetOwner(new SecurityIdentifier(ownerSid));
                    if (IsFolder)
                    {
                        Directory.SetAccessControl(FilePath, acs as DirectorySecurity);
                    }
                    else
                    {
                        File.SetAccessControl(FilePath, acs as FileSecurity);
                    }
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    // User does not have rights to set the owner
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        private static bool GetAccessControl(string filePath, bool isFolder, out FileSystemSecurity fss)
        {
            try
            {
                if (isFolder && Directory.Exists(filePath))
                {
                    fss = Directory.GetAccessControl(filePath);
                    return true;
                }
                else if (File.Exists(filePath))
                {
                    fss = File.GetAccessControl(filePath);
                    return true;
                }
                else
                {
                    // File or folder does not exists
                    fss = null;
                    return false;
                }
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

        public static async Task<string> OpenObjectPicker(long hwnd)
        {
            return await Win32API.StartSTATask(() =>
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
                    if (picker.ShowDialog(Win32API.Win32Window.FromLong(hwnd)) == DialogResult.OK)
                    {
                        try
                        {
                            var attribs = picker.SelectedObject.FetchedAttributes;
                            if (attribs.Any() && attribs[0] is byte[] objectSid)
                            {
                                return new SecurityIdentifier(objectSid, 0).Value;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                return null;
            });
        }
    }

    public class FileSystemAccessRule2
    {
        public AccessControlType AccessControlType { get; set; }
        public FileSystemRights FileSystemRights { get; set; }
        public string IdentityReference { get; set; }
        public bool IsInherited { get; set; }
        public InheritanceFlags InheritanceFlags { get; set; }
        public PropagationFlags PropagationFlags { get; set; }

        public static FileSystemAccessRule2 FromFileSystemAccessRule(FileSystemAccessRule rule)
        {
            return new FileSystemAccessRule2()
            {
                AccessControlType = rule.AccessControlType,
                FileSystemRights = rule.FileSystemRights,
                IsInherited = rule.IsInherited,
                IdentityReference = rule.IdentityReference.Value,
                InheritanceFlags = rule.InheritanceFlags,
                PropagationFlags = rule.PropagationFlags
            };
        }

        public FileSystemAccessRule ToFileSystemAccessRule()
        {
            return new FileSystemAccessRule(
                identity: new SecurityIdentifier(IdentityReference),
                fileSystemRights: FileSystemRights,
                inheritanceFlags: InheritanceFlags,
                propagationFlags: PropagationFlags,
                type: AccessControlType);
        }
    }
}
