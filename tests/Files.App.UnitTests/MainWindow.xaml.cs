// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.UnitTests
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			ExtendsContentIntoTitleBar = true;
		}
	}
}
