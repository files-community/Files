// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace Files.App.UserControls
{
	public sealed partial class FileIcon : UserControl
	{
		private SelectedItemsPropertiesViewModel _ViewModel;
		public SelectedItemsPropertiesViewModel ViewModel
		{
			get => _ViewModel;
			set
			{
				_ViewModel = value;

				if (value is null)
					return;

				if (ViewModel?.CustomIconSource is not null)
					CustomIconImageSource = new SvgImageSource(ViewModel.CustomIconSource);
			}
		}

		private double _ItemSize;
		public double ItemSize
		{
			get => _ItemSize;
			set
			{
				_ItemSize = value;
				LargerItemSize = _ItemSize + 2.0;
			}
		}

		private double LargerItemSize { get; set; }

		private static DependencyProperty FileIconImageSourceProperty { get; } = DependencyProperty.Register(nameof(FileIconImageSource), typeof(BitmapImage), typeof(FileIcon), null);

		private BitmapImage FileIconImageSource
		{
			get => GetValue(FileIconImageSourceProperty) as BitmapImage;
			set => SetValue(FileIconImageSourceProperty, value);
		}

		public static DependencyProperty FileIconImageDataProperty { get; } = DependencyProperty.Register(nameof(FileIconImageData), typeof(byte[]), typeof(FileIcon), null);

		public byte[] FileIconImageData
		{
			get => GetValue(FileIconImageDataProperty) as byte[];
			set
			{
				SetValue(FileIconImageDataProperty, value);
				if (value is not null)
					UpdateImageSourceAsync();
			}
		}

		private SvgImageSource CustomIconImageSource { get; set; }

		// Constructor

		public FileIcon()
		{
			InitializeComponent();
		}

		// Methods

		public async Task UpdateImageSourceAsync()
		{
			if (FileIconImageData is not null)
			{
				FileIconImageSource = new BitmapImage();
				using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
				await stream.WriteAsync(FileIconImageData.AsBuffer());
				stream.Seek(0);
				await FileIconImageSource.SetSourceAsync(stream);
			}
		}
	}
}