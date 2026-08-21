// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Controls;

public class WindowedDialog : Window
{
	private readonly WindowedDialogRoot _root;

	public WindowedDialog()
	{
		_root = new();
		Content = _root;
	}

	public FrameworkElement? Header
	{
		get => _root.Header;
		set => _root.Header = value;
	}

	public object RootContent
	{
		get => _root.Content;
		set => _root.Content = value;
	}

	public FrameworkElement? Footer
	{
		get => _root.Footer;
		set => _root.Footer = value;
	}
}
