using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml.Markup;

namespace Files.Helpers
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public sealed class ResourceString : MarkupExtension
    {
        public string Name { get; set; }

        protected override object ProvideValue() => Name.GetLocalized();
    }
}
