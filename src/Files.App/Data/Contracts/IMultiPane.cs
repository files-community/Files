// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract for multiple pane information.
	/// </summary>
	public interface IMultiPane
	{
		/// <summary>
		/// Gets the pane holder.
		/// </summary>
		public IPaneHolderPage PaneHolder { get; }
	}
}
