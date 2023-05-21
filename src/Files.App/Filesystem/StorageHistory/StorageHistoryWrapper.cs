// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.FilesystemHistory
{
	public class StorageHistoryWrapper : IDisposable
	{
		private int _index = -1;

		private List<IStorageHistory> _histories = new();

		public bool CanRedo()
		{
			return _index + 1 < _histories.Count;
		}

		public bool CanUndo()
		{
			return _index >= 0 && _histories.Count > 0;
		}

		public IStorageHistory GetCurrentHistory()
		{
			return _histories[_index];
		}

		public void AddHistory(IStorageHistory history)
		{
			if (history is not null)
			{
				++_index;

				_histories.Insert(_index, history);
				_histories.RemoveRange(_index + 1, _histories.Count - _index - 1);
			}
		}

		public void RemoveHistory(IStorageHistory history, bool decreaseIndex = true)
		{
			if (history is not null)
			{
				_histories.RemoveRange(_index + 1, _histories.Count - _index - 1);

				if (decreaseIndex)
					--_index;

				_histories.Remove(history);
			}
		}

		public void ModifyCurrentHistory(IStorageHistory newHistory)
		{
			_histories[_index].Modify(newHistory);
		}

		public void DecreaseIndex()
		{
			--_index;
		}

		public void IncreaseIndex()
		{
			++_index;
		}

		public void Dispose()
		{
			_histories = null;
		}
	}
}
