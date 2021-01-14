using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class CodePreview : UserControl
    {
        public CodePreview(ListedItem item)
        {
            this.InitializeComponent();
            SetFile(item);
        }

        public static List<string> Extensions => (new List<List<string>>(languageExtensions.Values).SelectMany(i => i).Distinct()).ToList();

        async void SetFile(ListedItem item)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            var text = await FileIO.ReadTextAsync(file);
            TextPreviewControl.Text = $"```{GetCodeLanguage(item.FileExtension)}\n{text}\n```";
        }

        static Dictionary<string, List<string>> languageExtensions = new Dictionary<string, List<string>>()
        {
            {"xml",  new List<string> {".xml", ".axml", ".xaml" } },
            {"json",  new List<string> {".json" } },
            {"python",  new List<string> {".py", ".py3" } },
            {"cs",  new List<string> {".cs" } },
            {"fs",  new List<string> {".fs" } },
            {"java",  new List<string> {".java" } },
            {"vb",  new List<string> {".vb" } },
            {"c",  new List<string> {".c" } },
            {"cpp",  new List<string> {".cpp" } },

        };

        static string GetCodeLanguage(string ext)
        {
            foreach (var lang in languageExtensions)
            {
                if(lang.Value.Contains(ext))
                {
                    return lang.Key;
                }
            }
            return ext;
        }
    }
}
