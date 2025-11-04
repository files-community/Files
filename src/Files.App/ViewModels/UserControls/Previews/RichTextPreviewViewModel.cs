// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Windows.Storage.Streams;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class RichTextPreviewViewModel : BasePreviewModel
	{
		public IRandomAccessStream Stream { get; set; }

		public RichTextPreviewViewModel(ListedItem item) : base(item) { }

		public static bool ContainsExtension(string extension)
			=> extension is ".rtf";

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			Stream = await Item.ItemFile.OpenReadAsync();

			return [];
		}

		public override void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
		{
			Stream?.Dispose();
			Stream = null;

			base.PreviewControlBase_Unloaded(sender, e);
		}
	}
}
