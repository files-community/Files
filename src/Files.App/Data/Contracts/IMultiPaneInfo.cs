// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract class for multiple pane info.
	/// </summary>
	public interface IMultiPaneInfo
	{
		public IPaneHolder PaneHolder { get; }
	}
}
