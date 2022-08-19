﻿using Files.Shared;
using Files.Shared.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.Uwp.Helpers
{
    public static class CommonPaths
    {
        public static readonly string DesktopPath = UserDataPaths.GetDefault().Desktop;

        public static readonly string DownloadsPath = UserDataPaths.GetDefault().Downloads;

        public static readonly string LocalAppDataPath = UserDataPaths.GetDefault().LocalAppData;

        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public static readonly string RecycleBinPath = Constants.CommonPaths.RecycleBinPath;

        public static readonly string NetworkFolderPath = Constants.CommonPaths.NetworkFolderPath;

        public static string MyComputerPath = Constants.CommonPaths.MyComputerPath;

        public static readonly string TempPath = ApplicationData.Current.LocalSettings.Values.Get("TEMP", "");

        public static readonly string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static Dictionary<string, string> ShellPlaces = new Dictionary<string, string>() {
            { "::{645FF040-5081-101B-9F08-00AA002F954E}", RecycleBinPath },
            { "::{5E5F29CE-E0A8-49D3-AF32-7A7BDC173478}", "Home".GetLocalized() /*MyComputerPath*/ },
            { "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", "Home".GetLocalized() /*MyComputerPath*/ },
            { "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", NetworkFolderPath },
            { "::{208D2C60-3AEA-1069-A2D7-08002B30309D}", NetworkFolderPath },
            { RecycleBinPath.ToUpperInvariant(), RecycleBinPath },
            { MyComputerPath.ToUpperInvariant(), "Home".GetLocalized() /*MyComputerPath*/ },
            { NetworkFolderPath.ToUpperInvariant(), NetworkFolderPath },
        };
    }
}
