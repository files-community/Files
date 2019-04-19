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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
