using ColorCode;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Files.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Previews
{
	public class CodePreviewViewModel : BasePreviewModel
	{
		private static readonly Lazy<IReadOnlyDictionary<string, ILanguage>> extensions = new(GetDictionary);

		private string textValue;
		public string TextValue
		{
			get => textValue;
			private set => SetProperty(ref textValue, value);
		}

		private ILanguage codeLanguage;
		public ILanguage CodeLanguage
		{
			get => codeLanguage;
			private set => SetProperty(ref codeLanguage, value);
		}

		public CodePreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public static bool ContainsExtension(string extension)
			=> extensions.Value.ContainsKey(extension);

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();

			try
			{
				var text = TextValue ?? await ReadFileAsTextAsync(Item.ItemFile);
				details.Add(GetFileProperty("PropertyLineCount", text.Split('\n').Length));

				CodeLanguage = extensions.Value[Item.FileExtension.ToLowerInvariant()];
				TextValue = text.Left(Core.Constants.PreviewPane.TextCharacterLimit);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			return details;
		}

		private static IReadOnlyDictionary<string, ILanguage> GetDictionary()
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

			return new ReadOnlyDictionary<string, ILanguage>(dictionary);
		}
	}
}
