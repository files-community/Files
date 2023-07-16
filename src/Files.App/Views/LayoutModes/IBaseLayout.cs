// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Views.LayoutModes
{
	public interface IBaseLayout : IDisposable
	{
		IBaseLayoutViewModel BaseViewModel { get; }
	}
}
