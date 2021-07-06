using ColorCode;
using Files.Extensions;
using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class CodePreviewViewModel : BasePreviewModel
    {
        public CodePreviewViewModel(ListedItem item) : base(item)
        {
        }

        private string textValue;
        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        private ILanguage codeLanguage;
        public ILanguage CodeLanguage
        {
            get => codeLanguage;
            set => SetProperty(ref codeLanguage, value);
        }

        public static List<string> Extensions => new List<List<string>>(languageExtensions.Values).SelectMany(i => i).Distinct().ToList();

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();

            try
            {
                var text = TextValue ?? await FileIO.ReadTextAsync(Item.ItemFile);
                CodeLanguage = GetCodeLanguage(Item.FileExtension);

                details.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = text.Split("\n").Length,
                });

                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                TextValue = displayText;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return details;
        }

        private static Dictionary<ILanguage, List<string>> languageExtensions = new Dictionary<ILanguage, List<string>>()
        {
            {Languages.Xml,  new List<string> {".xml", ".axml", ".xaml"} },
            {Languages.CSharp,  new List<string> {".cs", ".cake", ".csx", ".linq"} },
            {Languages.Html,  new List<string> {".razor", ".cshtml", ".vbhtml", ".svelte"} },
            {Languages.AspxCs,  new List<string> { ".acsx" } },
            {Languages.FSharp,  new List<string> {".fs", ".fsi", ".fsx" } },
            {Languages.Java,  new List<string> {".java" } },
            {Languages.VbDotNet,  new List<string> {".vb", ".vbs" } },
            {Languages.Cpp,  new List<string> {".cpp", ".c++", ".cc", ".cp", ".cxx", ".h", ".h++", ".hh", ".hpp", ".hxx", ".inc", ".inl", ".ino", ".ipp", ".re", ".tcc", ".tpp" } },
            {Languages.PowerShell,  new List<string> {".pwsh", ".ps1", ".psd1", ".psm1" } },
            {Languages.Typescript,  new List<string> {".ts", ".tsx"} },
            {Languages.JavaScript,  new List<string> {".js", ".jsx"} },
            {Languages.Php,  new List<string> {".php"} },
            {Languages.Css,  new List<string> {".css", ".scss"} },
            {Languages.Haskell,  new List<string> {".hs"} },
            {Languages.Aspx,  new List<string> {".aspx"} },
        };

        private static ILanguage GetCodeLanguage(string ext) => languageExtensions.FirstOrDefault(x => x.Value.Contains(ext)).Key;
    }
}
