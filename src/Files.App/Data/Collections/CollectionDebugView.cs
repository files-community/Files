// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Collections
{
	internal sealed class CollectionDebugView<T>
	{
		private readonly ICollection<T> _collection;

		public CollectionDebugView(ICollection<T> collection)
		{
			_collection = collection ?? throw new ArgumentNullException(nameof(collection));
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				var items = new T[_collection.Count];
				_collection.CopyTo(items, 0);

				return items;
			}
		}
	}
}
