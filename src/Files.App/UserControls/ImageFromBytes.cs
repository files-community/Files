// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
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
			DependencyProperty.RegisterAttached("SourceBytes", typeof(byte[]), typeof(ImageFromBytes), new PropertyMetadata(null, OnSourceBytesChangedAsync));

		private static async void OnSourceBytesChangedAsync(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Image image)
			{
				image.Source = await ((byte[])e.NewValue).ToBitmapAsync();
			}
		}
	}
}
