using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class CodePreview : PreviewControlBase
    {
        public CodePreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }

        public static List<string> Extensions => (new List<List<string>>(languageExtensions.Values).SelectMany(i => i).Distinct()).ToList();

        public override async void LoadPreviewAndDetails()
        {
            try
            {
                var text = await FileIO.ReadTextAsync(ItemFile);
                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                // Use the MarkDownTextBlock's built in code highlighting
                TextPreviewControl.Text = $"```{GetCodeLanguage(Item.FileExtension)}\n{displayText}\n```";

                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = text.Split("\n").Length,
                });

            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            base.LoadSystemFileProperties();
        }

        static Dictionary<string, List<string>> languageExtensions = new Dictionary<string, List<string>>()
        {
            // TODO: Debug color issue then reenable xml support
            //{"xml",  new List<string> {".xml", ".axml", ".xaml" } },
            {"json",  new List<string> {".json" } },
            {"yaml", new List<string> {".yml"} },
            {"python",  new List<string> {".py", ".py3", ".py", ".cgi", ".fcgi", ".gyp", ".gypi", ".lmi", ".py3", ".pyde", ".pyi", ".pyp", ".pyt", ".pyw", ".rpy", ".smk", ".spec", ".tac", ".wsgi", ".xpy" } },
            {"cs",  new List<string> {".cs", ".cake", ".csx", ".linq" } },
            {"fs",  new List<string> {".fs", "fsi", "fsx" } },
            {"java",  new List<string> {".java" } },
            {"vbnet",  new List<string> {".vb", "vbhtml" } },
            {"c",  new List<string> {".c" } },
            {"cpp",  new List<string> {".cpp", "c++", ".cc", ".cp", ".cxx", ".h", ".h++", ".hh", ".hpp", ".hxx", ".inc", ".inl", ".ino", ".ipp", ".re", ".tcc", ".tpp" } },
            {"powershell",  new List<string> {".pwsh", ".ps1", "psd1", ".psm1" } },
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
