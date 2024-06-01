// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Vanara.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.Views
{
	public sealed partial class UITestsWindow : Window, INotifyPropertyChanged
	{
		public List<string> ThemedIconStyleNames = [];

		private int _ThemedIconStyleIndex = 0;
		public int ThemedIconStyleIndex
		{
			get => _ThemedIconStyleIndex;
			set
			{
				_ThemedIconStyleIndex = value;
				ThemedIconStyle = (Style)Application.Current.Resources[$"App.ThemedIcons.{ThemedIconStyleNames[value]}"];
				NotifyPropertyChanged(nameof(ThemedIconStyleIndex));
			}
		}

		private Style? _ThemedIconStyle = (Style)Application.Current.Resources[$"App.ThemedIcons.Empty"];
		public Style? ThemedIconStyle
		{
			get => _ThemedIconStyle;
			set
			{
				_ThemedIconStyle = null;
				NotifyPropertyChanged(nameof(ThemedIconStyle));
				_ThemedIconStyle = value;
				NotifyPropertyChanged(nameof(ThemedIconStyle));
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public UITestsWindow()
		{
			ThemedIconStyleNames = Enum.GetNames(typeof(ThemedIconStyles)).ToList();

			InitializeComponent();
			ExtendsContentIntoTitleBar = true;
		}

		private void SelectionIteratorButton_Click(object sender, RoutedEventArgs e)
		{
			ThemedIconStyleIndex = _ThemedIconStyleIndex + 1;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
