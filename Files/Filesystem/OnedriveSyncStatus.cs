using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public enum OnedriveSyncStatus
    {
        NotOneDrive = -2,
        Unknown = -1,
        Folder_Online = 0,
        Folder_Offline_Partial = 1,
        Folder_Offline_Full = 2,
        Folder_Offline_Pinned = 3,
        Folder_Excluded = 4,
        Folder_Empty = 5,
        File_Sync_Upload = 6,
        File_Online = 8,
        File_Sync_Download = 9,
        File_Offline = 14,
        File_Offline_Pinned = 15,
    }
}
