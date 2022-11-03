using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.Resources;

namespace Files.App.Helpers
{
	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public sealed class ResourceString : MarkupExtension
	{
		private static ResourceLoader resourceLoader = new ResourceLoader();

		public string Name
		{
			get; set;
		}

		protected override object ProvideValue()
		{
			return resourceLoader.GetString(this.Name);
		}
	}
}
