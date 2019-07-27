using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace ExecutableLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
            Process.Start(executable);
        }
    }
}
