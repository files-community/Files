using Microsoft.UI.Xaml.Markup;

namespace Files.App.Helpers.XamlHelpers
{
	public class SystemTypeToXaml : MarkupExtension
	{
		private object parameter;

		public int Int { set => parameter = value; }

		public double Double { set => parameter = value; }

		public float Float { set => parameter = value; }

		public bool Bool { set => parameter = value; }

		protected override object ProvideValue()
			=> parameter;
	}
}