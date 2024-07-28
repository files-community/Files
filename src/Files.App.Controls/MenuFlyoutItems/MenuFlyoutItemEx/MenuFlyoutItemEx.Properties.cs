using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
    public partial class MenuFlyoutItemEx : MenuFlyoutItem
    {
        #region ThemedIcon (Style)
        public static readonly DependencyProperty ThemedIconProperty =
            DependencyProperty.Register(
                nameof(ThemedIcon),
                typeof(Style),
                typeof(MenuFlyoutItemEx),
                new PropertyMetadata(null, OnThemedIconChanged));



        public Style ThemedIcon
        {
            get { return (Style)GetValue( ThemedIconProperty ); }
            set { SetValue( ThemedIconProperty , value );  } 
        }



        private static void OnThemedIconChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            var control = (MenuFlyoutItemEx)d;

            if ( e.NewValue != null )
            {
                if ( e.NewValue != e.OldValue )
                {
                    control._useThemedIcon = true;

                    control.ThemedIconChanged(d, (Style)e.NewValue);
                }
            }
            else 
            { 
                control._useThemedIcon = false; 
            }
        }
        #endregion


        #region IconSize (Double)
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(MenuFlyoutItemEx),
                new PropertyMetadata((double)16, OnIconSizePropertyChanged));



        public double IconSize
        {
            get => (double)GetValue( IconSizeProperty );
            set => SetValue( IconSizeProperty , value );
        }



        private static void OnIconSizePropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            var control = (MenuFlyoutItemEx)d;

            if ( e.NewValue != null )
            {
                if ( e.NewValue != e.OldValue )
                { 
                    control.IconSizeChanged(d, (double)e.NewValue);
                }
            }
        }		

        #endregion
    }
}
