﻿using System;
using Windows.Storage;

namespace Files.Helpers
{
    public static class CommonPaths
    {
        public static readonly string DesktopPath = UserDataPaths.GetDefault().Desktop;

        public static readonly string DownloadsPath = UserDataPaths.GetDefault().Downloads;

        public static readonly string LocalAppDataPath = UserDataPaths.GetDefault().LocalAppData;

        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public static readonly string RecycleBinPath = Constants.CommonPaths.RecycleBinPath;

        public static readonly string NetworkFolderPath = Constants.CommonPaths.NetworkFolderPath;

        public static readonly string TempPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null);

        public static readonly string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
