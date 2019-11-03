using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ProcessLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
            var arguments = (string)ApplicationData.Current.LocalSettings.Values["Arguments"];
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executable;
            if(!string.IsNullOrWhiteSpace(arguments))
            {
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = arguments;
            }
            else
            {
                process.StartInfo.CreateNoWindow = true;
            }
            process.Start();            
        }
    }
}
