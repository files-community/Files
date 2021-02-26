using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class CodePreviewViewModel : BasePreviewModel
    {
        public CodePreviewViewModel(ListedItem item) : base(item)
        {
        }

        public string textValue;

        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public static List<string> Extensions => new List<List<string>>(languageExtensions.Values).SelectMany(i => i).Distinct().ToList();

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();
            try
            {
                var text = await FileIO.ReadTextAsync(Item.ItemFile);
                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                // Use the MarkDownTextBlock's built in code highlighting
                TextValue = $"```{GetCodeLanguage(Item.FileExtension)}\n{displayText}\n```";

                details.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = text.Split("\n").Length,
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return details;
        }

        private static Dictionary<string, List<string>> languageExtensions = new Dictionary<string, List<string>>()
        {
            // TODO: Debug color issue then re-enable xml support
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

        private static string GetCodeLanguage(string ext)
        {
            foreach (var lang in languageExtensions)
            {
                if (lang.Value.Contains(ext))
                {
                    return lang.Key;
                }
            }

            return ext;
        }
    }
}