// Copyright (c) Files Community
// Licensed under the MIT License.

using ColorCode;
using Files.App.ViewModels.Properties;
using Files.Shared.Helpers;
using System.Collections.Frozen;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class CodePreviewViewModel : BasePreviewModel
	{
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

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();

			try
			{
				var text = TextValue ?? await ReadFileAsTextAsync(Item.ItemFile);
				details.Add(GetFileProperty("PropertyLineCount", text.Split('\n').Length));

				CodeLanguage = FileExtensionHelpers.CodeFileExtensions[Item.FileExtension.ToLowerInvariant()];
				TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			return details;
		}
	}
}
