using Windows.UI.Xaml.Markup;

namespace Files.Uwp.Helpers.XamlHelpers
{
    public class SystemTypeToXaml : MarkupExtension
    {
        #region Private Members

        private object parameter;

        #endregion Private Members

        #region Public Properties

        public int Int { set => parameter = value; }

        public double Double { set => parameter = value; }

        public float Float { set => parameter = value; }

        public bool Bool { set => parameter = value; }

        #endregion Public Properties

        protected override object ProvideValue()
        {
            return parameter;
        }
    }
}