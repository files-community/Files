using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.Windows.Shell;

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

        [DllImport("shell32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr FindExecutable(string lpFile, string lpDirectory, [Out] StringBuilder lpResult);

        public static async Task<string> GetFileAssociation(string filename)
        {
            // Find UWP apps
            var uwp_apps = await Windows.System.Launcher.FindFileHandlersAsync(System.IO.Path.GetExtension(filename));
            if (uwp_apps.Any()) return uwp_apps.First().PackageFamilyName;
            // Find desktop apps
            var lpResult = new StringBuilder();
            var hResult = FindExecutable(filename, null, lpResult);
            if (hResult.ToInt64() > 32) return lpResult.ToString();
            return null;
        }

        public enum PropertyReturnType
        {
            RAWVALUE,
            DISPLAYVALUE
        }

        public static List<(Vanara.PInvoke.Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)> RecyledFileProperties =
            new List<(Vanara.PInvoke.Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)>
        {
            (Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size, PropertyReturnType.RAWVALUE),
            (Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size, PropertyReturnType.DISPLAYVALUE),
            (Vanara.PInvoke.Ole32.PROPERTYKEY.System.ItemTypeText, PropertyReturnType.RAWVALUE),
            (PropertyStore.GetPropertyKeyFromName("System.Recycle.DateDeleted"), PropertyReturnType.RAWVALUE)
        };

        // A faster method of getting file shell properties (currently non used)
        public static IList<object> GetFileProperties(ShellItem folderItem, List<(Vanara.PInvoke.Ole32.PROPERTYKEY propertyKey, PropertyReturnType returnType)> properties)
        {
            var propValueList = new List<object>(properties.Count);
            var flags = Vanara.PInvoke.PropSys.GETPROPERTYSTOREFLAGS.GPS_DEFAULT | Vanara.PInvoke.PropSys.GETPROPERTYSTOREFLAGS.GPS_FASTPROPERTIESONLY;

            Vanara.PInvoke.PropSys.IPropertyStore pStore = null;
            try
            {
                pStore = ((Vanara.PInvoke.Shell32.IShellItem2)folderItem.IShellItem).GetPropertyStoreForKeys(properties.Select(p => p.propertyKey).ToArray(), (uint)properties.Count, flags, typeof(Vanara.PInvoke.PropSys.IPropertyStore).GUID);
                foreach (var prop in properties)
                {
                    using var propVariant = new Vanara.PInvoke.Ole32.PROPVARIANT();
                    pStore.GetValue(prop.propertyKey, propVariant);
                    if (prop.returnType == PropertyReturnType.RAWVALUE)
                    {
                        propValueList.Add(propVariant.Value);
                    }
                    else if (prop.returnType == PropertyReturnType.DISPLAYVALUE)
                    {
                        using var pDesc = PropertyDescription.Create(prop.propertyKey);
                        var pValue = pDesc?.FormatForDisplay(propVariant, Vanara.PInvoke.PropSys.PROPDESC_FORMAT_FLAGS.PDFF_DEFAULT);
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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int LoadString(IntPtr hInstance, int ID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static string ExtractStringFromDLL(string file, int number)
        {
            IntPtr lib = LoadLibrary(file);
            StringBuilder result = new StringBuilder(2048);
            LoadString(lib, number, result, result.Capacity);
            FreeLibrary(lib);
            return result.ToString();
        }

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);

        public static string[] CommandLineToArgs(string commandLine)
        {
            if (String.IsNullOrEmpty(commandLine))
                return Array.Empty<string>();

            var argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
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
    }
}