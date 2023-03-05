using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Files.App.ViewModels.UserControls.Previews
{
	public class RichTextPreviewViewModel : BasePreviewModel
	{
		public IRandomAccessStream Stream { get; set; }

		public RichTextPreviewViewModel(ListedItem item) : base(item) { }

		public static bool ContainsExtension(string extension)
			=> extension is ".rtf";

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			Stream = await Item.ItemFile.OpenReadAsync();

			return new List<FileProperty>();
		}
	}
}
