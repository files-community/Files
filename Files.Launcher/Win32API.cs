using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Vanara.PInvoke;
using Windows.System;
using System.IO;

namespace FilesFullTrust
{
    internal class Win32API
    {
        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static async Task<string> GetFileAssociation(string filename)
        {
            // Find UWP apps
            var uwp_apps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
            if (uwp_apps.Any())
            {
                return uwp_apps.First().PackageFamilyName;
            }

            // Find desktop apps
            var lpResult = new StringBuilder();
            var hResult = Shell32.FindExecutable(filename, null, lpResult);
            if (hResult.ToInt64() > 32)
            {
                return lpResult.ToString();
            }

            return null;
        }

        public enum PropertyReturnType
        {
            RAWVALUE,
            DISPLAYVALUE
        }

        public static List<(Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)> RecyledFileProperties =
            new List<(Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)>
        {
            (Ole32.PROPERTYKEY.System.Size, PropertyReturnType.RAWVALUE),
            (Ole32.PROPERTYKEY.System.Size, PropertyReturnType.DISPLAYVALUE),
            (Ole32.PROPERTYKEY.System.ItemTypeText, PropertyReturnType.RAWVALUE),
            (PropertyStore.GetPropertyKeyFromName("System.Recycle.DateDeleted"), PropertyReturnType.RAWVALUE)
        };

        // A faster method of getting file shell properties (currently non used)
        public static IList<object> GetFileProperties(ShellItem folderItem, List<(Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)> properties)
        {
            var propValueList = new List<object>(properties.Count);
            var flags = PropSys.GETPROPERTYSTOREFLAGS.GPS_DEFAULT | PropSys.GETPROPERTYSTOREFLAGS.GPS_FASTPROPERTIESONLY;

            PropSys.IPropertyStore pStore = null;
            try
            {
                pStore = ((Shell32.IShellItem2)folderItem.IShellItem).GetPropertyStoreForKeys(properties.Select(p => p.propertyKey).ToArray(), (uint)properties.Count, flags, typeof(PropSys.IPropertyStore).GUID);
                foreach (var prop in properties)
                {
                    using var propVariant = new Ole32.PROPVARIANT();
                    pStore.GetValue(prop.propertyKey, propVariant);
                    if (prop.returnType == PropertyReturnType.RAWVALUE)
                    {
                        propValueList.Add(propVariant.Value);
                    }
                    else if (prop.returnType == PropertyReturnType.DISPLAYVALUE)
                    {
                        using var pDesc = PropertyDescription.Create(prop.propertyKey);
                        var pValue = pDesc?.FormatForDisplay(propVariant, PropSys.PROPDESC_FORMAT_FLAGS.PDFF_DEFAULT);
                        propValueList.Add(pValue);
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(pStore);
            }

            return propValueList;
        }

        public static string ExtractStringFromDLL(string file, int number)
        {
            var lib = Kernel32.LoadLibrary(file);
            StringBuilder result = new StringBuilder(2048);
            User32.LoadString(lib, number, result, result.Capacity);
            Kernel32.FreeLibrary(lib);
            return result.ToString();
        }

        public static string[] CommandLineToArgs(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return Array.Empty<string>();
            }

            var argv = Shell32.CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static void UnlockBitlockerDrive(string drive, string password)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-command \"$SecureString = ConvertTo-SecureString '{password}' -AsPlainText -Force; Unlock-BitLocker -MountPoint '{drive}' -Password $SecureString\"";
                process.Start();
                process.WaitForExit(30 * 1000);
            }
            catch (Win32Exception)
            {
                // If user cancels UAC
            }
        }
    }
}