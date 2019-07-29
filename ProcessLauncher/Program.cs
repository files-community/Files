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
            var dir = (string)ApplicationData.Current.LocalSettings.Values["StartDir"];
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = executable;
            if(dir != "")
            {
                process.StartInfo.WorkingDirectory = dir;
            }
            process.Start();            
        }
    }
}
