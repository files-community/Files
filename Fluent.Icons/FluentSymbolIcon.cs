using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Fluent.Icons
{
    public partial class FluentSymbolIcon : Control
    {
        private PathIcon iconDisplay;

        public FluentSymbolIcon()
        {
            this.DefaultStyleKey = typeof(FluentSymbolIcon);
        }

        /// <summary>
        /// Constructs a <see cref="FluentSymbolIcon"/> with the specified symbol.
        /// </summary>
        public FluentSymbolIcon(FluentSymbol symbol)
        {
            this.DefaultStyleKey = typeof(FluentSymbolIcon);
            Symbol = symbol;
        }

        /// <summary>
        /// Gets or sets the Fluent System Icons glyph used as the icon content.
        /// </summary>
        public FluentSymbol Symbol
        {
            get { return (FluentSymbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Symbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
            "Symbol",
            typeof(FluentSymbol), typeof(FluentSymbolIcon),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSymbolChanged))
        );

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.GetTemplateChild("IconDisplay") is PathIcon pi)
            {
                this.iconDisplay = pi;
                // Awkward workaround for a weird bug where iconDisplay is null
                // when OnSymbolChanged fires in a newly created FluentSymbolIcon
                Symbol = Symbol;
            }
        }

        private static void OnSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluentSymbolIcon self && (e.NewValue is FluentSymbol || e.NewValue is int) && self.iconDisplay != null)
            {
                // Set internal Image to the SvgImageSource from the look-up table
                self.iconDisplay.Data = GetPathData((FluentSymbol)e.NewValue);
            }
        }

        /// <summary>
        /// Returns a new <see cref="PathIcon"/> using the path associated with the provided <see cref="FluentSymbol"/>.
        /// </summary>
        public static PathIcon GetPathIcon(FluentSymbol symbol)
        {
            return new PathIcon {
                Data = (Geometry)Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), GetPathData(symbol)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Returns a new <see cref="Geometry"/> using the path associated with the provided <see cref="int"/>.
        /// The <paramref name="symbol"/> parameter is cast to <see cref="FluentSymbol"/>.
        /// </summary>
        public static Geometry GetPathData(int symbol)
        {
            return GetPathData((FluentSymbol)symbol);
        }

        /// <summary>
        /// Returns a new <see cref="Geometry"/> using the path associated with the provided <see cref="int"/>.
        /// </summary>
        public static Geometry GetPathData(FluentSymbol symbol)
        {
            if (AllFluentIcons.TryGetValue(symbol, out string pathData))
            {
                return (Geometry)Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
            }
            else
            {
                return null;
            }
        }
    }
}
