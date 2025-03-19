// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UITests.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Files.App.UITests.Views
{
	public sealed partial class OmnibarPage : Page
	{
		private readonly ObservableCollection<DummyItem1> DummyItems1;

		public OmnibarPage()
		{
			InitializeComponent();

			DummyItems1 =
			[
				new("Open online help page in browser", "Open online help page in browser", "Control + H"),
				new("Toggle full screen", "Toggle full screen", "Control + H"),
				new("Enter compact overlay", "Enter compact overlay", "Control + H"),
				new("Toggle compact overlay", "Toggle compact overlay", "Control + H"),
				new("Go to search box", "Go to search box", "Control + H"),
				new("Focus path bar", "Focus path bar", "Control + H"),
				new("Redo the last file operation", "Redo the last file operation", "Control + H"),
				new("Undo the last file operation", "Undo the last file operation", "Control + H"),
				new("Toggle whether to show hidden items", "Toggle whether to show hidden items", "Control + H"),
			];
		}
	}
}
