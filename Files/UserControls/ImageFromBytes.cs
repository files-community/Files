﻿using Files.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public class ImageFromBytes : DependencyObject
    {
        public static byte[] GetSourceBytes(DependencyObject obj)
        {
            return (byte[])obj.GetValue(SourceBytesProperty);
        }

        public static void SetSourceBytes(DependencyObject obj, byte[] value)
        {
            obj.SetValue(SourceBytesProperty, value);
        }

        public static readonly DependencyProperty SourceBytesProperty =
            DependencyProperty.RegisterAttached("SourceBytes", typeof(byte[]), typeof(ImageFromBytes), new PropertyMetadata(null, OnSourceBytesChanged));

        private static async void OnSourceBytesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image)
            {
                image.Source = await ((byte[])e.NewValue).ToBitmapAsync();
            }
        }
    }
}
