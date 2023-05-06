// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }
	}
}
