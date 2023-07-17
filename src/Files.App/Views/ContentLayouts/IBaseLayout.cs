// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Views.ContentLayouts
{
	public interface IBaseLayout : IDisposable
	{
		IBaseLayoutViewModel BaseViewModel { get; }
	}
}
