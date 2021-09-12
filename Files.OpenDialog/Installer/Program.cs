using System;
using System.Diagnostics;
using System.IO;

namespace Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strProjectPath = Path.GetDirectoryName(strExeFilePath);

            if (args.Length < 1 || args[0].Equals("/i", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Setting Files as open file dialog");
                string strDestinationRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\49306atecsolution.FilesUWP_et10x9a9vyk8t\LocalState");
                if (!Directory.Exists(strDestinationRoot))
                {
                    Console.WriteLine("Installing in Files Debug");
                    strDestinationRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\FilesDev_et10x9a9vyk8t\LocalState");
                }
                if (!Directory.Exists(strDestinationRoot))
                {
                    Console.WriteLine("Files is not installed!");
                    return;
                }
                strDestinationRoot = Path.Combine(strDestinationRoot, "DialogLib");
                if (!Directory.Exists(strDestinationRoot))
                {
                    Directory.CreateDirectory(strDestinationRoot);
                }

                using var copyProcess = new Process();
                copyProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                copyProcess.StartInfo.FileName = "xcopy";
                copyProcess.StartInfo.Arguments = $"/s /y {Path.Combine(strProjectPath, "DialogLib")} {strDestinationRoot}";
                copyProcess.Start();
                copyProcess.WaitForExit();

                string applyTpl = File.ReadAllText(Path.Combine(strProjectPath, "apply_fileopen.tpl"));
                string applyStr = string.Format(applyTpl, Path.Combine(strDestinationRoot, "CustomOpenDialog.dll").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string applyRegPath = Path.Combine(strProjectPath, "apply_fileopen.reg");
                File.WriteAllText(applyRegPath, applyStr);

                try
                {
                    using var regeditProcess = Process.Start("regedit.exe", applyRegPath);
                    regeditProcess.WaitForExit();
                }
                catch
                {
                    // Canceled UAC
                }
            }
            else if (args[0].Equals("/u", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Restoring default open file dialog");
                try
                {
                    using var regeditProcess = Process.Start("regedit.exe", Path.Combine(strProjectPath, "remove_fileopen.reg"));
                    regeditProcess.WaitForExit();
                }
                catch
                {
                    // Canceled UAC
                }
            }
        }
    }
}
