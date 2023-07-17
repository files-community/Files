// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.ContentLayouts
{
	/// <summary>
	/// Represents the base class which every layout page must derive from
	/// </summary>
	public abstract class BaseLayout : Page, IBaseLayout
	{
		public IBaseLayoutViewModel BaseViewModel = null!;

		public BaseLayout()
		{
			InitializeComponent();
		}

		public void Dispose()
		{
		}
	}
}
