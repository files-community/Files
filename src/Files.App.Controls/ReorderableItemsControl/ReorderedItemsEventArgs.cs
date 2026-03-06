// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public sealed class ReorderedItemsEventArgs : EventArgs
	{
		public IReadOnlyList<int> NewIndexToOldIndexMap { get; }

		public ReorderedItemsEventArgs(IReadOnlyList<int> newIndexToOldIndexMap)
		{
			NewIndexToOldIndexMap = newIndexToOldIndexMap;
		}
	}
}
