// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the base page of Column View
	/// </summary>
	public sealed partial class ColumnViewBase : BaseLayout
	{
		private readonly ColumnViewBaseViewModel ViewModel;

		public ColumnViewBase() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<ColumnViewBaseViewModel>();
			BaseViewModel = ViewModel;
		}
	}
}
