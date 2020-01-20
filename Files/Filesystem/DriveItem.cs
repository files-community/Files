using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class DriveItem
    {
        public string glyph { get; set; }
        public ulong maxSpace { get; set; } = 0;
        public ulong spaceUsed { get; set; } = 0;
        public string driveText { get; set; }
        public string tag { get; set; }
        public Visibility progressBarVisibility { get; set; }
        public string spaceText { get; set; }
        public Visibility cloudGlyphVisibility { get; set; } = Visibility.Collapsed;
        public Visibility driveGlyphVisibility { get; set; } = Visibility.Visible;
        public DriveType Type { get; set; }

        private StorageFolder _root;

        public DriveItem()
        {

        }

        public DriveItem(StorageFolder root, Visibility progressBarVisibility, DriveType type)
        {
	        _root = root;
	        this.progressBarVisibility = progressBarVisibility;
	        Type = type;

	        var properties = Task.Run(async () =>
	        {
		        return await root.Properties.RetrievePropertiesAsync(new[] {"System.FreeSpace", "System.Capacity"});
	        }).Result;

            

	        try
	        {
		        spaceUsed = maxSpace -
		                    Convert.ToUInt64(ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.FreeSpace"]).GigaBytes);
		        maxSpace = Convert.ToUInt64(ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.Capacity"]).GigaBytes);
		        spaceText = String.Format("{0} of {1}",
			        ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.FreeSpace"]).ToString(),
			        ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.Capacity"]).ToString());
            }
	        catch(NullReferenceException e)
	        {
		        spaceText = "Unkown";
	        }

	        driveText = root.DisplayName;

            tag = root.Path;

            switch (type)
	        {
		        case DriveType.Fixed:
			        glyph = "\uEDA2";
                    break;
		        case DriveType.Removable:
                    glyph = "\uE88E";
                    break;
		        case DriveType.Network:
			        break;
		        case DriveType.Ram:
			        break;
		        case DriveType.CDRom:
			        glyph = "\uE958";
                    break;
		        case DriveType.Unkown:
			        break;
		        case DriveType.NoRootDirectory:
			        break;
		        case DriveType.VirtualDrive:
			        break;
                case DriveType.FloppyDisk:
	                glyph = "\uEDA2";
	                break;
		        default:
			        throw new ArgumentOutOfRangeException(nameof(type), type, null);
	        }
        }


    }

    public enum DriveType
    {
        Fixed,
        Removable,
        Network,
        Ram,
        CDRom,
        FloppyDisk,
        Unkown,
        NoRootDirectory,
        VirtualDrive
    }
}
