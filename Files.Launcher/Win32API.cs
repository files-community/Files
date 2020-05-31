using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.Windows.Shell;

namespace FilesFullTrust
{
    internal class Win32API
    {
        // TODO: remove this when updated library is released
        [DllImport("shell32.dll")]
        public static extern Vanara.PInvoke.HRESULT SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        // TODO: remove this when updated library is released
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
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
                    else if(prop.returnType == PropertyReturnType.DISPLAYVALUE)
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
    }
}
