// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using WinRT;

namespace Files.App.Controls;

public class WindowedDialog : Window
{
	private readonly WindowedDialogRoot _root;

	public WindowedDialog()
	{
		_root = new();
		base.Content = _root;
	}

	public new string Title
	{
		get => base.Title;
		set
		{
			base.Title = value;
			_root.Title = value;
		}
	}

	public new UIElement? Content
	{
		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		get => _root.Content as UIElement;
		set => _root.Content = value;
	}

	public FrameworkElement? Header
	{
		get => _root.Header;
		set => _root.Header = value;
	}

	public FrameworkElement? Footer
	{
		get => _root.Footer;
		set => _root.Footer = value;
	}
}
