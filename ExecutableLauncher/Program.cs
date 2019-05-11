using System;
using System.Diagnostics;
using Windows.Storage;

namespace ExecutableLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                Process.Start(executable);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Console.WriteLine("While most executables work now, UWP restrictions still prevent the execution of this file");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
