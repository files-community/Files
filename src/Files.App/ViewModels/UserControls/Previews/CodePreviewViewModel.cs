// Copyright (c) Files Community
// Licensed under the MIT License.

using ColorCode;
using ColorCode.Parsing;
using Files.App.ViewModels.Properties;
using System.Collections.Frozen;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class CodePreviewViewModel : BasePreviewModel
	{
		private static readonly FrozenDictionary<string, ILanguage> extensions = GetDictionary();

		private string? textValue;
		public string? TextValue
		{
			get => textValue;
			private set => SetProperty(ref textValue, value);
		}

		private ILanguage? codeLanguage;
		public ILanguage? CodeLanguage
		{
			get => codeLanguage;
			private set => SetProperty(ref codeLanguage, value);
		}

		public ILanguageParser? ParsedCode { get; private set; }

		public CodePreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public static bool IsCodeFile(string extension)
			=> extensions.ContainsKey(extension);

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();

			try
			{
				var text = TextValue ?? await ReadFileAsTextAsync(PreviewFile);
				details.Add(GetFileProperty("PropertyLineCount", text.Split('\n').Length));

				var extension = Item.FileExtension!.ToLowerInvariant();
				var language = extensions[extension];
				var previewText = text.Left(Constants.PreviewPane.TextCharacterLimit);
				CodeLanguage = language;
				TextValue = previewText;
				if (extension is not (".css" or ".scss"))
					ParsedCode = new CachedCodeParser(previewText, language);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			return details;
		}

		private sealed class CachedCodeParser : CodeColorizerBase, ILanguageParser
		{
			private readonly List<(string Text, IList<Scope> Scopes)> fragments = [];

			public CachedCodeParser(string text, ILanguage language) : base(null, null)
			{
				languageParser.Parse(text, language, Write);
			}

			protected override void Write(string parsedSourceCode, IList<Scope> scopes)
				=> fragments.Add((parsedSourceCode, scopes));

			void ILanguageParser.Parse(string sourceCode, ILanguage language, Action<string, IList<Scope>> parseHandler)
			{
				foreach (var (text, scopes) in fragments)
					parseHandler(text, scopes);
			}
		}

		private static FrozenDictionary<string, ILanguage> GetDictionary()
		{
			var items = new Dictionary<ILanguage, string>
			{
				[Languages.Aspx] = "aspx",
				[Languages.AspxCs] = "acsx",
				[Languages.Cpp] = "cpp,c++,cc,cp,cxx,h,h++,hh,hpp,hxx,inc,inl,ino,ipp,re,tcc,tpp",
				[Languages.CSharp] = "cs,cake,csx,linq",
				[Languages.Css] = "css,scss",
				[Languages.FSharp] = "fs,fsi,fsx",
				[Languages.Haskell] = "hs",
				[Languages.Html] = "razor,cshtml,vbhtml,svelte",
				[Languages.Java] = "java",
				[Languages.JavaScript] = "js,jsx",
				[Languages.Php] = "php",
				[Languages.PowerShell] = "pwsh,ps1,psd1,psm1",
				[Languages.Typescript] = "ts,tsx",
				[Languages.VbDotNet] = "vb,vbs",
				[Languages.Xml] = "xml,axml,xaml,xsd,xsl,xslt,xlf",
			};

			var dictionary = new Dictionary<string, ILanguage>();

			foreach (var item in items)
			{
				var extensions = item.Value.Split(',').Select(ext => $".{ext}");
				foreach (var extension in extensions)
				{
					dictionary.Add(extension, item.Key);
				}
			}

			return dictionary.ToFrozenDictionary();
		}
	}
}
