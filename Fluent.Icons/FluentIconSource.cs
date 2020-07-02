using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Fluent.Icons
{
    // NOTE: It appears that the only thing preventing this library from
    // having a minimum Win10 version of 16299 is PathIconSource.
    /// <summary>
    /// Represents an icon source that uses a Fluent System Icon as its content.
    /// </summary>
    public class FluentIconSource : PathIconSource
    {
        public FluentIconSource() { }

        /// <summary>
        /// Constructs an icon source that uses Fluent System Icon as its content.
        /// </summary>
        public FluentIconSource(FluentSymbol symbol)
        {
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
            typeof(FluentSymbol), typeof(FluentIconSource),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSymbolChanged))
        );

        private static void OnSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluentIconSource self && (e.NewValue is FluentSymbol || e.NewValue is int))
            {
                // Set internal Image to the SvgImageSource from the look-up table
                self.Data = FluentSymbolIcon.GetPathData((FluentSymbol)e.NewValue);
            }
        }
    }
}
