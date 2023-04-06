using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace Files.App.UserControls
{
	public sealed partial class InvalidWarning : UserControl
	{
		public bool ShowWarning
		{
			get => (bool)GetValue(ShowWarningProperty);
			set => SetValue(ShowWarningProperty, value);
		}

		public readonly static DependencyProperty ShowWarningProperty =
			DependencyProperty.Register(nameof(ShowWarning), typeof(bool), typeof(InvalidWarning), new PropertyMetadata(false));

		public string TooltipText
		{
			get => (string)GetValue(TooltipTextProperty);
			set => SetValue(TooltipTextProperty, value);
		}

		public readonly static DependencyProperty TooltipTextProperty =
			DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(InvalidWarning), new PropertyMetadata(false));

		public string WarningMessage
		{
			get => (string)GetValue(WarningMessageProperty);
			set => SetValue(WarningMessageProperty, value);
		}

		public readonly static DependencyProperty WarningMessageProperty =
			DependencyProperty.Register(nameof(WarningMessage), typeof(string), typeof(InvalidWarning), new PropertyMetadata(false));

		public InvalidWarning()
		{
			InitializeComponent();
		}
	}
}
